using System;
using System.Threading;
using System.Threading.Tasks;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BachMitID.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IMitIdAccountService _mitIdAccountService;
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;

        public AuthController(
            IMitIdAccountService mitIdAccountService,
            IKafkaProducer kafkaProducer,
            ILogger<AuthController> logger,
            IConfiguration config)
        {
            _mitIdAccountService = mitIdAccountService;
            _kafkaProducer = kafkaProducer;
            _logger = logger;
            _config = config;
        }

        // /auth/login?accountId=...&returnUrl=/wallet
        [HttpGet("login")]
        public IActionResult Login([FromQuery] Guid accountId, [FromQuery] string? returnUrl = "/wallet")
        {
            if (accountId == Guid.Empty)
                return BadRequest("Missing accountId");

            // anti-open-redirect (kun interne paths)
            if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/"))
                returnUrl = "/wallet";

            // ✅ vi bærer accountId + returnUrl videre til /auth/result
            var redirectAfterLogin =
                $"/auth/result?accountId={accountId}&returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Challenge(
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    RedirectUri = redirectAfterLogin
                },
                "oidc"
            );
        }

        [Authorize]
        [HttpGet("result")]
        public async Task<IActionResult> Result([FromQuery] Guid accountId, [FromQuery] string? returnUrl = "/wallet", CancellationToken ct = default)
        {
            if (accountId == Guid.Empty)
                return BadRequest("Missing accountId");

            if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/"))
                returnUrl = "/wallet";

            // 1) Opret / hent MitID-account ud fra claims og gem i DB (linked til accountId)
            var result = await _mitIdAccountService.CreateFromClaimsAsync(User, accountId);

            if (result == null)
                return BadRequest("Could not create MitID account from claims.");

            var dto = result.Account;

            // 2) Kun send 'created'-event hvis account rent faktisk er ny
            if (result.IsNew)
            {
                try
                {
                    var @event = new MitIdVerified(
                        AccountId: dto.AccountId,
                        IsAdult: dto.IsAdult,
                        VerifiedAt: DateTime.UtcNow
                    );

                    await _kafkaProducer.PublishAsync(
                        topic: Topics.MitIdVerified,
                        key: dto.AccountId.ToString(),
                        message: @event,
                        ct: ct
                    );

                    _logger.LogInformation("Published MitIdVerified for AccountId {AccountId}", dto.AccountId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish MitIdVerified. Continuing without event.");
                }
            }

            // 3) Redirect tilbage til Wallet frontend med claims
            var walletBase = (_config["Wallet:BaseUrl"] ?? "http://localhost:8081").TrimEnd('/');

            var query = $"?action=issue_token&isAdult={dto.IsAdult.ToString().ToLower()}&subId={dto.SubId}";
            return Redirect(walletBase + returnUrl + query);
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
