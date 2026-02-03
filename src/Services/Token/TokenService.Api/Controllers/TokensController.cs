using Api.Contracts;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/tokens")]
    [Authorize]
    public class TokensController : ControllerBase
    {
        private readonly ITokenIssuanceService _tokenService;
        private readonly IMapper _mapper;

        public TokensController(ITokenIssuanceService tokenService, IMapper mapper)
        {
            _tokenService = tokenService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<IssueTokenResponseContract>> IssueToken(
            [FromBody] IssueTokenRequestContract contract)
        {
            var dto = _mapper.Map<IssueTokenDto>(contract);

            var result = await _tokenService.IssueTokenAsync(dto, HttpContext.RequestAborted);

            var response = _mapper.Map<IssueTokenResponseContract>(result);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "token-service",
                timestamp = DateTime.UtcNow
            });
        }
    }
}
