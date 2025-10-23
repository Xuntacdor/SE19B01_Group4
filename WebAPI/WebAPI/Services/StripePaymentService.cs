using Stripe;
using Stripe.Checkout;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services.Payments
{
    public sealed class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IVipPlanRepository _vipPlanRepo;

        public StripePaymentService(IConfiguration config, IVipPlanRepository vipPlanRepo)
        {
            _config = config;
            _vipPlanRepo = vipPlanRepo;
        }

        public string CreateVipCheckoutSession(int planId, int userId)
        {
            var plan = _vipPlanRepo.GetById(planId);
            if (plan == null)
                throw new InvalidOperationException($"VIP plan {planId} not found.");

            // Cấu hình URL
            var domain = _config["Stripe:Domain"];
            var successUrl = _config["Stripe:SuccessUrl"];
            var cancelUrl = _config["Stripe:CancelUrl"];

            if (string.IsNullOrWhiteSpace(successUrl) && !string.IsNullOrWhiteSpace(domain))
                successUrl = $"{domain.TrimEnd('/')}/payment-success";
            if (string.IsNullOrWhiteSpace(cancelUrl) && !string.IsNullOrWhiteSpace(domain))
                cancelUrl = $"{domain.TrimEnd('/')}/payment-cancel";

            if (string.IsNullOrWhiteSpace(successUrl) || string.IsNullOrWhiteSpace(cancelUrl))
                throw new InvalidOperationException("Stripe SuccessUrl/CancelUrl (or Domain) is not configured.");

            // Currency & amount
            var currency = (_config["Stripe:Currency"] ?? "vnd").ToLowerInvariant();
            long unitAmount = IsZeroDecimalCurrency(currency)
                ? (long)Math.Round(plan.Price, 0)
                : (long)(plan.Price * 100m);

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = $"{domain.TrimEnd('/')}/payment-success?success=true",
                CancelUrl = $"{domain.TrimEnd('/')}/payment-success?canceled=true",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = unitAmount,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = plan.PlanName ?? $"VIP Plan {plan.VipPlanId}",
                                Description = plan.Description
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "planId", plan.VipPlanId.ToString() }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);
            return session.Url ?? string.Empty;
        }

        private static bool IsZeroDecimalCurrency(string ccy)
        {
            return ccy is "bif" or "clp" or "djf" or "gnf" or "jpy" or "kmf"
                       or "krw" or "mga" or "pyg" or "rwf" or "ugx" or "vnd"
                       or "vuv" or "xaf" or "xof" or "xpf";
        }
    }
}
