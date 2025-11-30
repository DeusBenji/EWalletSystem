using AutoMapper;
using BachMitID.Domain.Interfaces;
using BachMitID.Domain.Model;
using BachMitID.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BachMitID.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccDbAccess _accDb;
        private readonly IMapper _mapper;

        public AccountsController(IAccDbAccess accDb, IMapper mapper)
        {
            _accDb = accDb;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<AccountDto>> Create([FromBody] AccountDto dto)
        {
            var entity = _mapper.Map<Account>(dto);
            var newId = await _accDb.CreateAccount(entity);

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AccountDto>> GetById(Guid id)
        {
            var entity = await _accDb.GetAccountById(id);
            if (entity == null)
                return NotFound();

            return Ok(_mapper.Map<AccountDto>(entity));
        }
    }
}
