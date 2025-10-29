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
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            IStripeWebhookService webhookService,
            IConfiguration config,
            ILogger<StripeWebhookController> logger)
        {
            _webhookService = webhookService;
            _config = config;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleAsync()
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var json = await reader.ReadToEndAsync();

            var endpointSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(endpointSecret))
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "WebhookSecret is not configured." });

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    endpointSecret
                );

                _webhookService.ProcessWebhook(stripeEvent);

                return Ok(new { message = "Webhook processed successfully." });
            }
            catch (StripeException e)
            {
                _logger.LogError(e, "[StripeWebhook] Invalid signature or parse error: {Message}", e.Message);
                return BadRequest(new { error = "Invalid Stripe signature or malformed payload." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[StripeWebhook] Unexpected error: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
