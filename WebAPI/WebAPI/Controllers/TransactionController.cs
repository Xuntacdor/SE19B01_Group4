using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public sealed class TransactionController : ControllerBase
    {
        private readonly ITransactionService _service;

        public TransactionController(ITransactionService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetPaged([FromQuery] TransactionDTO query)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var result = _service.GetPaged(query, user.UserId, user.IsAdmin);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var tx = _service.GetById(id, user.UserId, user.IsAdmin);
            if (tx == null) return NotFound();
            return Ok(tx);
        }

        [HttpPost]
        public IActionResult Post([FromBody] TransactionDTO dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            try
            {
                var created = _service.CreateOrGetByReference(dto, user.UserId);
                return CreatedAtAction(nameof(GetById), new { id = created.TransactionId }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:int}/cancel")]
        public IActionResult Cancel(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            try
            {
                var updated = _service.Cancel(id, user.UserId, user.IsAdmin);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{id:int}/refund")]
        public IActionResult Refund(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            try
            {
                var updated = _service.Refund(id, user.UserId, user.IsAdmin);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = "admin")]
        public IActionResult Approve(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            try
            {
                var updated = _service.Approve(id, user.UserId, user.IsAdmin);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
        }
        [HttpPost("create")]
        [Authorize]
        public IActionResult CreateTransaction([FromBody] TransactionDTO dto)
        {
            var userContext = GetCurrentUser();
            if (userContext == null)
                return Unauthorized("User not logged in.");

            var created = _service.CreateVipTransaction(dto, userContext.UserId);
            return Ok(created);
        }


        [HttpGet("export")]
        public IActionResult Export([FromQuery] TransactionDTO query)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var bytes = _service.ExportCsv(query, user.UserId, user.IsAdmin);
            var fileName = $"transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private UserContext? GetCurrentUser()
        {
            var uid = HttpContext.Session.GetInt32("UserId");
            if (uid != null)
            {
                var role = HttpContext.Session.GetString("Role");
                return new UserContext(uid.Value, role == "admin");
            }

            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claimId == null) return null;

            if (int.TryParse(claimId, out int id))
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                return new UserContext(id, role?.ToLower() == "admin");
            }

            return null;
        }

        public sealed record UserContext(int UserId, bool IsAdmin);
    }
}
