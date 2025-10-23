using Microsoft.AspNetCore.Mvc;
using Stripe;
using WebAPI.Services.Webhooks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/stripe/webhook")]
    public sealed class StripeWebhookController : ControllerBase
    {
        private readonly IStripeWebhookService _webhookService;
        private readonly IConfiguration _config;

        public StripeWebhookController(IStripeWebhookService webhookService, IConfiguration config)
        {
            _webhookService = webhookService;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> HandleAsync()
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync();

            // Lấy Webhook Secret từ cấu hình (appsettings hoặc biến môi trường)
            var endpointSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(endpointSecret))
                return StatusCode(StatusCodes.Status500InternalServerError, "WebhookSecret is not configured.");

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );

                // Gọi service xử lý webhook (đồng bộ)
                _webhookService.ProcessWebhook(stripeEvent);

                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"[Stripe] Signature or parse error: {e.Message}");
                return BadRequest();
            }
        }
    }
}
