using Api.Contracts;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ValidationController : ControllerBase
    {
        private readonly ICredentialValidationService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            ICredentialValidationService service,
            IMapper mapper,
            ILogger<ValidationController> logger)
        {
            _service = service;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("verify")]
        [ProducesResponseType(typeof(VerifyCredentialResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(VerifyCredentialResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<VerifyCredentialResponse>> Verify([FromBody] VerifyCredentialRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.VcJwt))
            {
                return BadRequest(new VerifyCredentialResponse
                {
                    Success = false,
                    FailureReason = "VC JWT must be provided",
                    VerifiedAt = DateTime.UtcNow
                });
            }

            var dto = _mapper.Map<VerifyCredentialDto>(request);

            var result = await _service.VerifyAsync(dto);

            var response = _mapper.Map<VerifyCredentialResponse>(result);

            if (!result.IsValid)
                return BadRequest(response);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "validation-service",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
