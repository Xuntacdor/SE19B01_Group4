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
        private readonly Mock<IVipAuthorizationService> _vipAuthServiceMock;
        private readonly VIPAuthorizationHandler _handler;
        private readonly VIPRequirement _requirement;

        public VIPAuthorizationHandlerTests()
        {
            _vipAuthServiceMock = new Mock<IVipAuthorizationService>();
            _handler = new VIPAuthorizationHandler(_vipAuthServiceMock.Object);
            _requirement = new VIPRequirement();
        }

        private static AuthorizationHandlerContext CreateContextWithClaims(Claim? claim)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                claim != null ? new[] { claim } : new Claim[] { }));
            return new AuthorizationHandlerContext(
                new[] { new VIPRequirement() }, user, null);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenNoClaim_ShouldNotSucceed()
        {
            // Arrange
            var context = CreateContextWithClaims(null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
            _vipAuthServiceMock.Verify(x => x.IsUserVip(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenClaimNotInteger_ShouldNotSucceed()
        {
            // Arrange
            var claim = new Claim(ClaimTypes.NameIdentifier, "invalid");
            var context = CreateContextWithClaims(claim);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
            _vipAuthServiceMock.Verify(x => x.IsUserVip(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenUserIsNotVip_ShouldNotSucceed()
        {
            // Arrange
            var claim = new Claim(ClaimTypes.NameIdentifier, "5");
            var context = CreateContextWithClaims(claim);
            _vipAuthServiceMock.Setup(x => x.IsUserVip(5)).Returns(false);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
            _vipAuthServiceMock.Verify(x => x.IsUserVip(5), Times.Once);
        }

        [Fact]
        public async Task HandleRequirementAsync_WhenUserIsVip_ShouldSucceed()
        {
            // Arrange
            var claim = new Claim(ClaimTypes.NameIdentifier, "42");
            var context = CreateContextWithClaims(claim);
            _vipAuthServiceMock.Setup(x => x.IsUserVip(42)).Returns(true);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
            _vipAuthServiceMock.Verify(x => x.IsUserVip(42), Times.Once);
        }

        [Fact]
        public void VIPRequirement_ShouldBeAssignableToIAuthorizationRequirement()
        {
            typeof(VIPRequirement)
                .Should()
                .Implement<IAuthorizationRequirement>();
        }
    }
}
