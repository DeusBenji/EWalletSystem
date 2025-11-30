using AutoMapper;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.DTOs;
using BachMitID.Application.Security;
using BachMitID.Domain.Interfaces;
using BachMitID.Domain.Model;
using System.Security.Claims;

namespace BachMitID.Application.BusinessLogicLayer
{
    public class MitIdAccountService : IMitIdAccountService
    {
        private readonly IMitIdDbAccess _mitIdDbAccess;
        private readonly IMapper _mapper;
        private readonly IMitIdAccountCache _cache;
         
        public MitIdAccountService(
            IMitIdDbAccess mitIdDbAccess,
            IMapper mapper,
            IMitIdAccountCache cache)
        {
            _mitIdDbAccess = mitIdDbAccess;
            _mapper = mapper;
            _cache = cache;
        }

        // -------- CREATE FROM CLAIMS (MitID login) --------
        public async Task<MitIdAccountDto?> CreateFromClaimsAsync(ClaimsPrincipal user)
        {
            // 1) Find sub (unik MitID-id)
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(sub))
                return null;

            // 2) Hent fødselsdato fra claims
            var dobClaim = user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth");
            if (dobClaim == null)
                return null;

            if (!DateTime.TryParse(dobClaim.Value, out var dob))
                return null;

            // 3) Beregn alder
            var today = DateTime.Today;
            var age = today.Year - dob.Year;
            if (dob > today.AddYears(-age)) age--;
            bool isAdult = age >= 18;

            Guid testId = Guid.Empty; // TODO: erstattes med rigtig AccountID når Account-service er koblet på

            // Hash sub før vi gemmer
            var hashedSub = SubIdHasher.Hash(sub);

            // 4) Lav DTO
            var dto = new MitIdAccountDto
            {
                Id = Guid.NewGuid(),   // bliver overskrevet efter DB-kald
                AccountId = testId,
                SubId = hashedSub,
                IsAdult = isAdult
            };

            // 5) DTO → domain
            var entity = _mapper.Map<MitID_Account>(dto);

            // 6) Gem i DB
            var newId = await _mitIdDbAccess.CreateMitIdAccount(entity);

            // brug ID'et vi fik (løsning A)
            dto.Id = newId;

            // 7) Læg i cache
            await _cache.SetAsync(dto, TimeSpan.FromMinutes(30));

            return dto;
        }

        // -------- READ --------
        public async Task<MitIdAccountDto?> GetByAccountIdAsync(Guid accountId)
        {
            // 1) Prøv cachen først
            var cached = await _cache.GetAsync(accountId);
            if (cached != null)
                return cached;

            // 2) Ellers DB
            var entity = await _mitIdDbAccess.GetMitIdAccountByAccId(accountId);
            if (entity == null)
                return null;

            var dto = _mapper.Map<MitIdAccountDto>(entity);

            // 3) Gem i cache til næste gang (fx 30 min)
            await _cache.SetAsync(dto, TimeSpan.FromMinutes(30));

            return dto;
        }

        public async Task<List<MitIdAccountDto>> GetAllAsync()
        {
            var entities = await _mitIdDbAccess.GetAllMitIdAccounts();
            return _mapper.Map<List<MitIdAccountDto>>(entities);
        }

        // -------- UPDATE --------
        public async Task<bool> UpdateAsync(Guid id, MitIdAccountDto dto)
        {
            var entity = _mapper.Map<MitID_Account>(dto);
            entity.ID = id;

            var updated = await _mitIdDbAccess.UpdateMitIdAccount(entity);

            if (updated)
            {
                // opdater cache med den nye version
                dto.Id = id;
                await _cache.SetAsync(dto, TimeSpan.FromMinutes(30));
            }

            return updated;
        }

        // -------- DELETE --------
        public async Task<bool> DeleteAsync(Guid id)
        {
            var deleted = await _mitIdDbAccess.DeleteMitIdAccount(id);

            if (deleted)
            {
                // ryd cache for denne account
                await _cache.RemoveAsync(id);
            }

            return deleted;
        }
    }
}
