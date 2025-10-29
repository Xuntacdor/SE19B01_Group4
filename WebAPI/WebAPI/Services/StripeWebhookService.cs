using Stripe;
using Stripe.Checkout;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services.Webhooks
{
    public sealed class StripeWebhookService : IStripeWebhookService
    {
        private readonly IUserRepository _userRepo;
        private readonly IVipPlanRepository _planRepo;
        private readonly ITransactionRepository _txnRepo;
        private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "bif","clp","djf","gnf","jpy","kmf","krw","mga","pyg","rwf",
            "ugx","vnd","vuv","xaf","xof","xpf"
        };
        public StripeWebhookService(IUserRepository userRepo, IVipPlanRepository planRepo, ITransactionRepository txnRepo)
        {
            _userRepo = userRepo;
            _planRepo = planRepo;
            _txnRepo = txnRepo;
        }
        public void ProcessWebhook(Event stripeEvent)
        {
            Console.WriteLine($"[StripeWebhook] Received event: {stripeEvent.Type}");
            var handler = stripeEvent.Type switch
            {
                "checkout.session.completed" => () => HandleCheckoutCompleted(stripeEvent),
                "payment_intent.canceled" => () => HandlePaymentCanceled(stripeEvent),
                "charge.refunded" => () => HandleChargeRefunded(stripeEvent),
                _ => (Action)(() => Console.WriteLine($"[StripeWebhook] Ignored event: {stripeEvent.Type}"))
            };
            handler();
        }
        private void HandleCheckoutCompleted(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not Session session) return;
            if (!TryGetInt(session.Metadata, "userId", out var userId) ||
                !TryGetInt(session.Metadata, "planId", out var planId)) return;
            var user = _userRepo.GetById(userId);
            var plan = _planRepo.GetById(planId);
            if (user == null || plan == null) return;

            var currency = (session.Currency ?? "vnd").ToUpperInvariant();
            var amount = session.AmountTotal.HasValue
                ? (IsZeroDecimal(currency)
                    ? session.AmountTotal.Value
                    : session.AmountTotal.Value / 100m)
                : plan.Price;
            var txn = _txnRepo.GetByReference(session.PaymentIntentId);
            if (txn == null)
            {
                _txnRepo.Add(new Transaction
                {
                    UserId = user.UserId,
                    PlanId = plan.VipPlanId,
                    Amount = amount,
                    Currency = currency,
                    PaymentMethod = "Stripe",
                    ProviderTxnId = session.PaymentIntentId,
                    Purpose = "VIP",
                    Status = "PAID",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                txn.Status = "PAID";
                txn.Amount = amount;
                txn.Currency = currency;
                _txnRepo.Update(txn);
            }
            UpdateUserVip(user, plan.DurationDays);
            _txnRepo.SaveChanges();
        }
        private void HandlePaymentCanceled(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not PaymentIntent pi || pi.Id == null) return;
            var txn = _txnRepo.GetByReference(pi.Id);
            if (txn == null) return;
            txn.Status = "FAILED";
            _txnRepo.Update(txn);
            _txnRepo.SaveChanges();
        }
        private void HandleChargeRefunded(Event stripeEvent)
        {
            if (stripeEvent.Data.Object is not Charge charge || charge.PaymentIntentId == null) return;
            var txn = _txnRepo.GetByReference(charge.PaymentIntentId);
            if (txn == null) return;
            txn.Status = "REFUNDED";
            _txnRepo.Update(txn);
            _txnRepo.SaveChanges();
        }
        private void UpdateUserVip(User user, int duration)
        {
            var now = DateTime.UtcNow;
            user.VipExpireAt = user.VipExpireAt.HasValue && user.VipExpireAt.Value > now
                ? user.VipExpireAt.Value.AddDays(duration)
                : now.AddDays(duration);
            _userRepo.Update(user);
        }
        private static bool TryGetInt(IReadOnlyDictionary<string, string> meta, string key, out int value)
        {
            value = 0;
            if (meta == null) return false;
            if (!meta.TryGetValue(key, out var s)) return false;
            return int.TryParse(s, out value);
        }
        private static bool IsZeroDecimal(string currency)
            => ZeroDecimalCurrencies.Contains(currency);
    }
}
