using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/vip")]
    [Authorize]
    public sealed class VipPaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public VipPaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public sealed class CreateVipCheckoutRequest
        {
            public int PlanId { get; set; }
            // Nếu chưa có JWT/Identity, cho phép truyền tạm UserId để test
            public int? UserId { get; set; }
        }

        public sealed class CreateVipCheckoutResponse
        {
            public string SessionUrl { get; set; } = string.Empty;
        }

        [HttpPost("pay")]
        public ActionResult<CreateVipCheckoutResponse> CreateVipCheckout([FromBody] CreateVipCheckoutRequest req)
        {
            // Lấy userId từ Claims; nếu chưa có auth, fallback body để test sandbox
            int? userId = null;
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimId, out var parsed)) userId = parsed;
            if (userId is null && req.UserId is not null) userId = req.UserId;

            if (userId is null)
                return BadRequest("UserId is required (from claims or request body for testing).");

            if (req.PlanId <= 0)
                return BadRequest("Invalid plan id.");

            // Gọi service xử lý đồng bộ
            var sessionUrl = _paymentService.CreateVipCheckoutSession(req.PlanId, userId.Value);

            return Ok(new CreateVipCheckoutResponse { SessionUrl = sessionUrl });
        }
    }
}
