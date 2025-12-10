using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Domain.Models;
using Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.BusinessLogic
{
    public sealed class AccountService : IAccountService
    {
        private readonly IAccountRepository _repo;
        private readonly IPasswordHasher _hasher;
        private readonly IKafkaProducer _kafka;
        private readonly IAccountCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IAccountRepository repo,
            IPasswordHasher hasher,
            IKafkaProducer kafka,
            IAccountCache cache,
            IMapper mapper,
            ILogger<AccountService> logger)
        {
            _repo = repo;
            _hasher = hasher;
            _kafka = kafka;
            _cache = cache;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AccountDto> RegisterAccountAsync(
            RegisterAccountDto dto,
            CancellationToken ct = default)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await _repo.EmailExistsAsync(email, ct))
                throw new InvalidOperationException("Email is already registered.");

            string? hash = null;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                hash = _hasher.HashPassword(dto.Password);
            }

            var account = new Account(email, hash);

            await _repo.CreateAsync(account, ct);

            var dtoCached = _mapper.Map<AccountDto>(account);

            await _cache.SetAccountAsync(dtoCached, ct);

            try
            {
                await _kafka.PublishAsync(Topics.AccountCreated, new AccountCreated(
                    account.Id,
                    account.Email,
                    account.CreatedAt
                ), ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish account-created for {AccountId}", account.Id);
            }

            return dtoCached;
        }

        public async Task<AccountDto?> GetAccountByIdAsync(Guid id, CancellationToken ct = default)
        {
            var cached = await _cache.GetAccountAsync(id, ct);
            if (cached is not null)
                return cached;

            var account = await _repo.GetByIdAsync(id, ct);
            if (account is null)
                return null;

            var dto = _mapper.Map<AccountDto>(account);
            await _cache.SetAccountAsync(dto, ct);

            return dto;
        }

        public async Task<AccountDto?> GetAccountByEmailAsync(string email, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var account = await _repo.GetByEmailAsync(normalized, ct);

            return account is null ? null : _mapper.Map<AccountDto>(account);
        }

        public async Task<AuthenticateAccountResult> AuthenticateAsync(string email, string password, CancellationToken ct = default)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var account = await _repo.GetByEmailAsync(normalized, ct);

            if (account is null || account.PasswordHash is null)
                return new(false, null, "Invalid credentials");

            if (!_hasher.Verify(password, account.PasswordHash))
                return new(false, null, "Invalid credentials");

            if (!account.IsActive)
                return new(false, null, "Account is deactivated");

            return new(true, account.Id, null);
        }

        public async Task<bool> ChangePasswordAsync(Guid accountId, string oldPassword, string newPassword, CancellationToken ct = default)
        {
            var account = await _repo.GetByIdAsync(accountId, ct);
            if (account is null || account.PasswordHash is null)
                return false;

            if (!_hasher.Verify(oldPassword, account.PasswordHash))
                return false;

            var newHash = _hasher.HashPassword(newPassword);
            account.ChangePassword(newHash);

            await _repo.UpdateAsync(account, ct);
            await _cache.InvalidateAsync(accountId, ct);

            return true;
        }

        public Task<bool> RequestEmailChangeAsync(Guid accountId, string newEmail, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> ConfirmEmailChangeAsync(string token, CancellationToken ct = default)
        {
            return Task.FromResult(false);
        }

        public async Task<bool> DeactivateAccountAsync(Guid accountId, CancellationToken ct = default)
        {
            var account = await _repo.GetByIdAsync(accountId, ct);
            if (account is null)
                return false;

            account.Deactivate();
            await _repo.UpdateAsync(account, ct);
            await _cache.InvalidateAsync(accountId, ct);

            await _kafka.PublishAsync("account-deactivated", new { account.Id }, ct);

            return true;
        }
    }
}
