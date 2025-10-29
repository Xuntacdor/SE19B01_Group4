using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Stripe;
using Stripe.Checkout;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services.Webhooks;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class StripeWebhookServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IVipPlanRepository> _planRepoMock;
        private readonly Mock<ITransactionRepository> _txnRepoMock;
        private readonly StripeWebhookService _service;

        public StripeWebhookServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _planRepoMock = new Mock<IVipPlanRepository>();
            _txnRepoMock = new Mock<ITransactionRepository>();
            _service = new StripeWebhookService(_userRepoMock.Object, _planRepoMock.Object, _txnRepoMock.Object);
        }

        [Fact]
        public void IsZeroDecimal_ShouldReturnTrue_ForJPY()
        {
            var method = typeof(StripeWebhookService).GetMethod("IsZeroDecimal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            var result = (bool)method.Invoke(null, new object[] { "JPY" })!;
            result.Should().BeTrue();
        }

        [Fact]
        public void IsZeroDecimal_ShouldReturnFalse_ForUSD()
        {
            var method = typeof(StripeWebhookService).GetMethod("IsZeroDecimal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            var result = (bool)method.Invoke(null, new object[] { "USD" })!;
            result.Should().BeFalse();
        }

        [Fact]
        public void ProcessWebhook_ShouldIgnore_UnknownEventType()
        {
            var evt = new Event { Type = "unknown.event" };
            Action act = () => _service.ProcessWebhook(evt);
            act.Should().NotThrow();
        }

        [Fact]
        public void HandleCheckoutCompleted_ShouldAddTransaction_WhenNew()
        {
            var session = new Session
            {
                Metadata = new Dictionary<string, string>
                {
                    { "userId", "1" },
                    { "planId", "2" }
                },
                Currency = "vnd",
                AmountTotal = 5000,
                PaymentIntentId = "pi_123"
            };

            var evt = new Event { Type = "checkout.session.completed", Data = new EventData { Object = session } };

            var user = new User { UserId = 1 };
            var plan = new VipPlan { VipPlanId = 2, Price = 50, DurationDays = 30 };

            _userRepoMock.Setup(r => r.GetById(1)).Returns(user);
            _planRepoMock.Setup(r => r.GetById(2)).Returns(plan);
            _txnRepoMock.Setup(r => r.GetByReference("pi_123")).Returns((Transaction?)null);

            _service.ProcessWebhook(evt);

            _txnRepoMock.Verify(r => r.Add(It.Is<Transaction>(t =>
                t.UserId == 1 && t.PlanId == 2 && t.Status == "PAID")), Times.Once);
            _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
            _txnRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void HandlePaymentCanceled_ShouldUpdateFailedTransaction()
        {
            var pi = new PaymentIntent { Id = "pi_cancel" };
            var evt = new Event { Type = "payment_intent.canceled", Data = new EventData { Object = pi } };
            var txn = new Transaction { ProviderTxnId = "pi_cancel", Status = "PENDING" };

            _txnRepoMock.Setup(r => r.GetByReference("pi_cancel")).Returns(txn);

            _service.ProcessWebhook(evt);

            txn.Status.Should().Be("FAILED");
            _txnRepoMock.Verify(r => r.Update(txn), Times.Once);
            _txnRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void HandleChargeRefunded_ShouldUpdateRefundedTransaction()
        {
            var charge = new Charge { PaymentIntentId = "pi_refund" };
            var evt = new Event { Type = "charge.refunded", Data = new EventData { Object = charge } };
            var txn = new Transaction { ProviderTxnId = "pi_refund", Status = "PAID" };

            _txnRepoMock.Setup(r => r.GetByReference("pi_refund")).Returns(txn);

            _service.ProcessWebhook(evt);

            txn.Status.Should().Be("REFUNDED");
            _txnRepoMock.Verify(r => r.Update(txn), Times.Once);
            _txnRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void TryGetInt_ShouldParseSuccessfully()
        {
            var method = typeof(StripeWebhookService).GetMethod("TryGetInt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            var args = new object?[]
            {
                new Dictionary<string, string> { ["key"] = "123" },
                "key",
                null
            };
            var result = (bool)method.Invoke(null, args)!;
            result.Should().BeTrue();
        }

        [Fact]
        public void TryGetInt_ShouldReturnFalse_WhenMissingKey()
        {
            var method = typeof(StripeWebhookService).GetMethod("TryGetInt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            var args = new object?[]
            {
                new Dictionary<string, string>(),
                "key",
                null
            };
            var result = (bool)method.Invoke(null, args)!;
            result.Should().BeFalse();
        }
    }
}
