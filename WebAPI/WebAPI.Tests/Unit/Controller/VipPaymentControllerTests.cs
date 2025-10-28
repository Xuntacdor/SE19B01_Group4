using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using WebAPI.Controllers;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class VipPaymentControllerTests
    {
        private readonly Mock<IPaymentService> _paymentService;
        private readonly VipPaymentController _controller;

        public VipPaymentControllerTests()
        {
            _paymentService = new Mock<IPaymentService>();
            _controller = new VipPaymentController(_paymentService.Object);
            var context = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        private void SetUserIdClaim(int userId)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        [Fact]
        public void CreateVipCheckout_WithClaimUser_ReturnsOk()
        {
            SetUserIdClaim(10);
            _paymentService.Setup(s => s.CreateVipCheckoutSession(2, 10)).Returns("http://session");

            var result = _controller.CreateVipCheckout(new VipPaymentController.CreateVipCheckoutRequest { PlanId = 2 });

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            (ok!.Value as VipPaymentController.CreateVipCheckoutResponse)!.SessionUrl.Should().Be("http://session");
        }

        [Fact]
        public void CreateVipCheckout_WithBodyUser_ReturnsOk()
        {
            _paymentService.Setup(s => s.CreateVipCheckoutSession(3, 11)).Returns("http://s");

            var result = _controller.CreateVipCheckout(new VipPaymentController.CreateVipCheckoutRequest { PlanId = 3, UserId = 11 });

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void CreateVipCheckout_NoUser_ReturnsBadRequest()
        {
            var result = _controller.CreateVipCheckout(new VipPaymentController.CreateVipCheckoutRequest { PlanId = 1 });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void CreateVipCheckout_InvalidPlan_ReturnsBadRequest()
        {
            SetUserIdClaim(12);
            var result = _controller.CreateVipCheckout(new VipPaymentController.CreateVipCheckoutRequest { PlanId = 0 });

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}



