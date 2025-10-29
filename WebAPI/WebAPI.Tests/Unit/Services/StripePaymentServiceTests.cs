using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe.Checkout;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services.Payments;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class StripePaymentServiceTests 
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<IVipPlanRepository> _planRepoMock;
        private readonly Mock<SessionService> _sessionServiceMock;
        private readonly StripePaymentService _service;

        public StripePaymentServiceTests()
        {
            _configMock = new Mock<IConfiguration>();
            _planRepoMock = new Mock<IVipPlanRepository>();
            _sessionServiceMock = new Mock<SessionService>(null);

            var configData = new Dictionary<string, string?>
            {
                ["Stripe:Domain"] = "https://example.com",
                ["Stripe:Currency"] = "vnd",
                ["Stripe:SuccessUrl"] = "",
                ["Stripe:CancelUrl"] = ""
            };
            foreach (var kvp in configData)
                _configMock.Setup(x => x[kvp.Key]).Returns(kvp.Value);

            _service = new StripePaymentService(_configMock.Object, _planRepoMock.Object, _sessionServiceMock.Object);
        }

        [Fact]
        public void CreateVipCheckoutSession_ShouldThrow_WhenPlanNotFound()
        {
            _planRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((VipPlan)null!);
            Action act = () => _service.CreateVipCheckoutSession(1, 1);
            act.Should().Throw<InvalidOperationException>().WithMessage("VIP plan 1 not found.");
        }

        [Fact]
        public void CreateVipCheckoutSession_ShouldThrow_WhenUrlsMissing()
        {
            _planRepoMock.Setup(r => r.GetById(It.IsAny<int>()))
                .Returns(new VipPlan { VipPlanId = 1, Price = 100, PlanName = "VIP1" });
            _configMock.Setup(c => c["Stripe:Domain"]).Returns((string?)null);
            _configMock.Setup(c => c["Stripe:SuccessUrl"]).Returns((string?)null);
            _configMock.Setup(c => c["Stripe:CancelUrl"]).Returns((string?)null);

            Action act = () => _service.CreateVipCheckoutSession(1, 1);
            act.Should().Throw<InvalidOperationException>().WithMessage("*not configured*");
        }

        [Fact]
        public void IsZeroDecimalCurrency_ShouldReturnTrue_ForVND()
        {
            var method = typeof(StripePaymentService).GetMethod("IsZeroDecimalCurrency",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var result = (bool)method.Invoke(null, new object[] { "vnd" })!;
            result.Should().BeTrue();
        }

        [Fact]
        public void IsZeroDecimalCurrency_ShouldReturnFalse_ForUSD()
        {
            var method = typeof(StripePaymentService).GetMethod("IsZeroDecimalCurrency",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            var result = (bool)method.Invoke(null, new object[] { "usd" })!;
            result.Should().BeFalse();
        }

        [Fact]
        public void GetUrl_ShouldReturnDomainFallback_WhenMissingExplicitUrl()
        {
            var method = typeof(StripePaymentService).GetMethod("GetUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var result = (string)method.Invoke(_service, new object[] { "Stripe:MissingKey", "fallback" })!;
            result.Should().Be("https://example.com/fallback");
        }

        [Fact]
        public void CreateVipCheckoutSession_ShouldHandleNonZeroDecimalCurrency()
        {
            var plan = new VipPlan { VipPlanId = 2, PlanName = "Pro", Price = 50 };
            _planRepoMock.Setup(r => r.GetById(2)).Returns(plan);
            _configMock.Setup(c => c["Stripe:Currency"]).Returns("usd");

            _sessionServiceMock.Setup(s => s.Create(It.IsAny<SessionCreateOptions>(), null))
                .Returns(new Session { Url = "https://checkout.mock/123" });

            var result = _service.CreateVipCheckoutSession(2, 1);

            result.Should().Be("https://checkout.mock/123");
        }

        [Fact]
        public void CreateVipCheckoutSession_ShouldReturnEmpty_WhenSessionUrlIsNull()
        {
            var plan = new VipPlan { VipPlanId = 3, PlanName = "VIP", Price = 200 };
            _planRepoMock.Setup(r => r.GetById(3)).Returns(plan);

            _sessionServiceMock.Setup(s => s.Create(It.IsAny<SessionCreateOptions>(), null))
                .Returns((Session)null);

            var result = _service.CreateVipCheckoutSession(3, 7);
            result.Should().BeEmpty();
        }

        [Fact]
        public void CreateVipCheckoutSession_ShouldComputeZeroDecimalCurrencyProperly()
        {
            var plan = new VipPlan { VipPlanId = 5, Price = 123.456M };
            _planRepoMock.Setup(r => r.GetById(5)).Returns(plan);
            _configMock.Setup(c => c["Stripe:Currency"]).Returns("vnd");

            _sessionServiceMock.Setup(s => s.Create(It.IsAny<SessionCreateOptions>(), null))
                .Returns(new Session { Url = "https://mock.checkout/789" });

            var result = _service.CreateVipCheckoutSession(5, 10);

            result.Should().Be("https://mock.checkout/789");
        }
    }
}
