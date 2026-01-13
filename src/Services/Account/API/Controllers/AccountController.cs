using System;
using System.Threading;
using System.Threading.Tasks;
using AccountService.API.Contracts;
using AccountService.API.Security;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccountService.API.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IMapper _mapper;
        private readonly JwtTokenService _jwtTokenService;

        public AccountController(
            IAccountService service,
            ILogger<AccountController> logger,
            IMapper mapper,
            JwtTokenService jwtTokenService)
        {
            _service = service;
            _logger = logger;
            _mapper = mapper;
            _jwtTokenService = jwtTokenService;
        }

        // Public: man skal kunne oprette konto uden at være logget ind
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] AccountRegisterRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var dto = _mapper.Map<RegisterAccountDto>(request);

                var account = await _service.RegisterAccountAsync(dto, ct);

                var response = _mapper.Map<AccountResponse>(account);

                return CreatedAtAction(nameof(GetById),
                    new { id = response.Id },
                    response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed for {Email}", request.Email);
                return BadRequest(new { error = ex.Message });
            }
        }

        // Public: login (NU returnerer vi også JWT)
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AccountLoginRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _service.AuthenticateAsync(request.Email, request.Password, ct);

            if (!result.Success)
                return Unauthorized(new { error = result.Failure });

            // ✅ result.AccountId is Guid? (nullable)
            if (!result.AccountId.HasValue || result.AccountId.Value == Guid.Empty)
                return Unauthorized(new { error = "Login OK, but AccountId was invalid." });

            var accountId = result.AccountId.Value;

            var accessToken = _jwtTokenService.CreateToken(accountId);

            return Ok(new
            {
                accountId,
                accessToken
            });
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetAccountByIdAsync(id, ct);

            if (dto is null)
                return NotFound();

            var response = _mapper.Map<AccountResponse>(dto);

            return Ok(response);
        }

        [Authorize]
        [HttpGet("{id:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetAccountByIdAsync(id, ct);

            if (dto is null)
                return NotFound();

            var response = _mapper.Map<AccountStatusResponse>(dto);

            return Ok(response);
        }

        // Public health endpoint (også nice til docker healthchecks)
        [AllowAnonymous]
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "account-service",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
