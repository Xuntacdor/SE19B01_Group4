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

        public StripeWebhookService(
            IUserRepository userRepo,
            IVipPlanRepository planRepo,
            ITransactionRepository txnRepo)
        {
            _userRepo = userRepo;
            _planRepo = planRepo;
            _txnRepo = txnRepo;
        }

        public void ProcessWebhook(Event stripeEvent)
        {
            Console.WriteLine($"[StripeWebhook] Received event: {stripeEvent.Type}");

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case "payment_intent.canceled":
                    HandlePaymentIntentCanceled(stripeEvent);
                    break;

                case "charge.refunded":
                    HandleChargeRefunded(stripeEvent);
                    break;

                default:
                    Console.WriteLine($"[StripeWebhook] Ignored event type: {stripeEvent.Type}");
                    break;
            }
        }

        private void HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            if (!TryGetInt(session.Metadata, "userId", out var userId)) return;
            if (!TryGetInt(session.Metadata, "planId", out var planId)) return;

            var user = _userRepo.GetById(userId);
            var plan = _planRepo.GetById(planId);
            if (user == null || plan == null) return;

            var currency = (session.Currency ?? "vnd").ToUpperInvariant();
            decimal amountPaid = session.AmountTotal.HasValue
                ? (IsZeroDecimal(currency)
                    ? session.AmountTotal.Value
                    : session.AmountTotal.Value / 100m)
                : plan.Price;

            var existing = _txnRepo.GetByReference(session.PaymentIntentId);
            if (existing == null)
            {
                _txnRepo.Add(new Transaction
                {
                    UserId = user.UserId,
                    PlanId = plan.VipPlanId,
                    Amount = amountPaid,
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
                existing.Status = "PAID";
                existing.Amount = amountPaid;
                existing.Currency = currency;
                _txnRepo.Update(existing);
            }

            // ✅ Gia hạn VIP
            var now = DateTime.UtcNow;
            user.VipExpireAt = user.VipExpireAt.HasValue && user.VipExpireAt.Value > now
                ? user.VipExpireAt.Value.AddDays(plan.DurationDays)
                : now.AddDays(plan.DurationDays);

            Console.WriteLine($"[Webhook] Before save: user.VipExpireAt={user.VipExpireAt}");

            // ✅ Cập nhật user, nhưng KHÔNG save riêng
            _userRepo.Update(user);

            // ✅ Lưu toàn bộ cùng một DbContext
            _txnRepo.SaveChanges();

            Console.WriteLine("[Webhook] After save: vip_expire_at updated successfully!");
        }



        private void HandlePaymentIntentCanceled(Event stripeEvent)
        {
            var pi = stripeEvent.Data.Object as PaymentIntent;
            if (pi?.Id == null) return;

            var txn = _txnRepo.GetByReference(pi.Id);
            if (txn == null) return;

            txn.Status = "FAILED";
            _txnRepo.Update(txn);
            _txnRepo.SaveChanges();
        }

        private void HandleChargeRefunded(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge?.PaymentIntentId == null) return;

            var txn = _txnRepo.GetByReference(charge.PaymentIntentId);
            if (txn == null) return;

            txn.Status = "REFUNDED";
            _txnRepo.Update(txn);
            _txnRepo.SaveChanges();
        }

        private static bool TryGetInt(IReadOnlyDictionary<string, string> meta, string key, out int value)
        {
            value = 0;
            if (meta == null) return false;
            if (!meta.TryGetValue(key, out var s)) return false;
            return int.TryParse(s, out value);
        }

        private static bool IsZeroDecimal(string ccyUpper)
        {
            return ccyUpper is "BIF" or "CLP" or "DJF" or "GNF" or "JPY" or "KMF"
                               or "KRW" or "MGA" or "PYG" or "RWF" or "UGX" or "VND"
                               or "VUV" or "XAF" or "XOF" or "XPF";
        }
    }
}
