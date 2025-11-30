using BachMitID.Application.BusinessLogicLayer;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.Contracts;
using BachMitID.Infrastructure.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BachMitID.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMitIdAccountService _mitIdAccountService;
        private readonly IMitIdAccountEventPublisher _eventPublisher;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMitIdAccountService mitIdAccountService,
            IMitIdAccountEventPublisher eventPublisher,
            ILogger<AuthController> logger)
        {
            _mitIdAccountService = mitIdAccountService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return Challenge(
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/auth/result"
                },
                "oidc"
            );
        }

        [Authorize]
        [HttpGet("result")]
        public async Task<IActionResult> Result()
        {
            // 1) Opret / hent MitID-account ud fra claims og gem i DB
            var dto = await _mitIdAccountService.CreateFromClaimsAsync(User);

            if (dto == null)
                return BadRequest("Could not create MitID account from claims.");

            // 2) Prøv at sende event – men lad det IKKE vælte login hvis Kafka fejler
            try
            {
                var evt = new MitIdAccountCreatedEvent
                {
                    Id = dto.Id,
                    AccountId = dto.AccountId,
                    SubId = dto.SubId,
                    IsAdult = dto.IsAdult
                };

                await _eventPublisher.PublishCreatedAsync(evt);
                _logger.LogInformation("Published MitIdAccountCreatedEvent for AccountId {AccountId}", dto.AccountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish MitIdAccountCreatedEvent. Continuing without event.");
                // Vi failer IKKE login – vi returnerer stadig DTO
            }

            // 3) Returnér noget synligt (JSON) så du kan se det i browseren
            return Ok(dto);
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            return SignOut(
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = "/"
                },
                new[] { "Cookies", "oidc" }
            );
        }
    }
}
