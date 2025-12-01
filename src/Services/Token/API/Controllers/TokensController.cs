using Api.Contracts;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using Application.BusinessLogic;
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/tokens")]
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
    }
}