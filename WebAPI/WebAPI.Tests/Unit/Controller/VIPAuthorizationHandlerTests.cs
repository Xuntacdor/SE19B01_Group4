using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using WebAPI.Authorization;
using WebAPI.Services.Authorization;
using Xunit;

namespace WebAPI.Tests.Unit.Authorization
{
    public class VIPAuthorizationHandlerTests
    {
        private readonly Mock<IVipAuthorizationService> _vipAuthMock;
        private readonly VIPAuthorizationHandler _handler;
        private readonly VIPRequirement _requirement;

        public VIPAuthorizationHandlerTests()
        {
            _vipAuthMock = new Mock<IVipAuthorizationService>();
            _handler = new VIPAuthorizationHandler(_vipAuthMock.Object);
            _requirement = new VIPRequirement();
        }

        // ------------------------------------------------------------
        // TEST 1️⃣: Should succeed when user is VIP
        // ------------------------------------------------------------
        [Fact]
        public async Task HandleRequirementAsync_ShouldSucceed_WhenUserIsVIP()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "123") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
            var context = new AuthorizationHandlerContext(
                new[] { _requirement },
                user,
                null
            );

            _vipAuthMock.Setup(s => s.IsUserVip(123)).Returns(true);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
            _vipAuthMock.Verify(s => s.IsUserVip(123), Times.Once);
        }

        // ------------------------------------------------------------
        // TEST 2️⃣: Should not succeed when user is not VIP
        // ------------------------------------------------------------
        [Fact]
        public async Task HandleRequirementAsync_ShouldNotSucceed_WhenUserIsNotVIP()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "123") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
            var context = new AuthorizationHandlerContext(
                new[] { _requirement },
                user,
                null
            );

            _vipAuthMock.Setup(s => s.IsUserVip(123)).Returns(false);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
            _vipAuthMock.Verify(s => s.IsUserVip(123), Times.Once);
        }

        // ------------------------------------------------------------
        // TEST 3️⃣: Should do nothing when NameIdentifier claim missing
        // ------------------------------------------------------------
        [Fact]
        public async Task HandleRequirementAsync_ShouldNotCallService_WhenClaimMissing()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity()); // no claim
            var context = new AuthorizationHandlerContext(
                new[] { _requirement },
                user,
                null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
            _vipAuthMock.Verify(s => s.IsUserVip(It.IsAny<int>()), Times.Never);
        }

        // ------------------------------------------------------------
        // TEST 4️⃣: Should do nothing when claim value invalid
        // ------------------------------------------------------------
        [Fact]
        public async Task HandleRequirementAsync_ShouldNotCallService_WhenClaimInvalid()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-number") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
            var context = new AuthorizationHandlerContext(
                new[] { _requirement },
                user,
                null
            );

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
            _vipAuthMock.Verify(s => s.IsUserVip(It.IsAny<int>()), Times.Never);
        }
    }
}
