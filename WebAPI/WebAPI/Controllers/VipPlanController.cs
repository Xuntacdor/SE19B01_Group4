using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/vipplans")]
    public class VipPlanController : ControllerBase
    {
        private readonly IVipPlanService _service;

        public VipPlanController(IVipPlanService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var plans = _service.GetAll();
            return Ok(plans);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var plan = _service.GetById(id);
            if (plan == null) return NotFound();
            return Ok(plan);
        }
        [HttpPost]
        [Authorize(Roles = "admin")]
        public IActionResult Create([FromBody] VipPlanDTO dto)
        {
            var newPlan = _service.Create(dto);
            return CreatedAtAction(nameof(GetById), new { id = newPlan.VipPlanId }, newPlan);
        }

        //  UPDATE
        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        public IActionResult Update(int id, [FromBody] VipPlanDTO dto)
        {
            var updated = _service.Update(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        //  DELETE
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(int id)
        {
            var success = _service.Delete(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
