using Stripe;

namespace WebAPI.Services.Webhooks
{
    public interface IStripeWebhookService
    {
        void ProcessWebhook(Event stripeEvent);
    }
}
