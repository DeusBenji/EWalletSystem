using BachMitID.Application.BusinessLogicLayer;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace BachMitID.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MitIdAccountsController : ControllerBase
    {
        private readonly IMitIdAccountService _service;

        public MitIdAccountsController(IMitIdAccountService service)
        {
            _service = service;
        }

        [HttpGet("{accountId:guid}")]
        public async Task<ActionResult<MitIdAccountDto>> GetByAccountId(Guid accountId)
        {
            var dto = await _service.GetByAccountIdAsync(accountId);
            if (dto == null)
                return NotFound();

            return Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<List<MitIdAccountDto>>> GetAll()
        {
            var dtos = await _service.GetAllAsync();
            return Ok(dtos);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MitIdAccountDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
