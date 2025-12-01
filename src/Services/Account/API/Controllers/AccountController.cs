using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using AccountService.API.Contracts;

namespace AccountService.API.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IMapper _mapper;

        public AccountController(
            IAccountService service,
            ILogger<AccountController> logger,
            IMapper mapper)
        {
            _service = service;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] AccountRegisterRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                // API -> Application DTO via AutoMapper
                var dto = _mapper.Map<RegisterAccountDto>(request);

                // Application → Application DTO result
                var account = await _service.RegisterAccountAsync(dto, ct);

                // Application DTO → API Contract via AutoMapper
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AccountLoginRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _service.AuthenticateAsync(request.Email, request.Password, ct);

            if (!result.Success)
                return Unauthorized(new { error = result.Failure });

            return Ok(new { result.AccountId });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetAccountByIdAsync(id, ct);

            if (dto is null)
                return NotFound();

            // Application DTO -> API via AutoMapper
            var response = _mapper.Map<AccountResponse>(dto);

            return Ok(response);
        }

        [HttpGet("{id:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetAccountByIdAsync(id, ct);

            if (dto is null)
                return NotFound();

            // Application DTO -> API via AutoMapper
            var response = _mapper.Map<AccountStatusResponse>(dto);

            return Ok(response);
        }

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
