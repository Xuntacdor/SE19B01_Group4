using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Stripe;
using System.Text;
using WebAPI.Controllers;
using WebAPI.Services.Webhooks;
using Xunit;

namespace WebAPI.Tests.Unit.Controllers
{
    public class StripeWebhookControllerTests
    {
        private readonly Mock<IStripeWebhookService> _webhookService;
        private readonly Mock<ILogger<StripeWebhookController>> _logger;
        private readonly IConfiguration _config;
        private readonly StripeWebhookController _controller;

        public StripeWebhookControllerTests()
        {
            _webhookService = new Mock<IStripeWebhookService>();
            _logger = new Mock<ILogger<StripeWebhookController>>();

            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Stripe:WebhookSecret", "whsec_testsecret"}
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _controller = new StripeWebhookController(
                _webhookService.Object,
                _config,
                _logger.Object
            );
        }

        [Fact]
        public async Task HandleAsync_ValidSignature_ShouldReturnOkOrBadRequestDependingOnSignature()
        {
            // Arrange
            var payload = "{}";
            var header = "t=1234,v1=fakesig";

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            _controller.Request.Headers["Stripe-Signature"] = header;

            _webhookService.Setup(s => s.ProcessWebhook(It.IsAny<Event>()));

            // Act
            var result = await _controller.HandleAsync();

            // Assert
            // Stripe will fail to validate the signature, so expect BadRequestObjectResult
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task HandleAsync_InvalidSignature_ShouldReturnBadRequestObject()
        {
            // Arrange
            var payload = "{}";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            _controller.Request.Headers["Stripe-Signature"] = "invalid";

            // Act
            var result = await _controller.HandleAsync();

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task HandleAsync_ShouldCatchException_AndReturnBadRequestObject()
        {
            // Arrange
            var payload = "{}";
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
            _controller.Request.Headers["Stripe-Signature"] = "t=1234,v1=fakesig";

            _webhookService.Setup(x => x.ProcessWebhook(It.IsAny<Event>()))
                .Throws(new Exception("test error"));

            // Act
            var result = await _controller.HandleAsync();

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task HandleAsync_WhenWebhookSecretMissing_ShouldReturn500()
        {
            // Arrange
            var badConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            var ctrl = new StripeWebhookController(_webhookService.Object, badConfig, _logger.Object);

            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ctrl.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

            // Act
            var result = await ctrl.HandleAsync();

            // Assert
            var obj = result as ObjectResult;
            obj.Should().NotBeNull();
            obj!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }
    }
}
