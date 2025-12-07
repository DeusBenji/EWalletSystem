using System;
using System.Threading;
using System.Threading.Tasks;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.DTOs;
using BuildingBlocks.Contracts.Events;
using BuildingBlocks.Contracts.Messaging;
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
        private readonly IKafkaProducer _kafkaProducer;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IMitIdAccountService mitIdAccountService,
            IKafkaProducer kafkaProducer,
            ILogger<AuthController> logger)
        {
            _mitIdAccountService = mitIdAccountService;
            _kafkaProducer = kafkaProducer;
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
        public async Task<IActionResult> Result(CancellationToken ct = default)
        {
            // 1) Opret / hent MitID-account ud fra claims og gem i DB
            var result = await _mitIdAccountService.CreateFromClaimsAsync(User);

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

                    _logger.LogInformation(
                        "Published MitIdVerified for AccountId {AccountId}",
                        dto.AccountId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish MitIdVerified. Continuing without event."
                    );
                    // login må stadig ikke fejle pga Kafka
                }
            }
            else
            {
                _logger.LogInformation(
                    "MitID account already exists for AccountId {AccountId}. No 'created' event published.",
                    dto.AccountId
                );
            }

            // 3) Returnér DTO til klienten
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
