using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services.Authorization;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class VipAuthorizationServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo;
        private readonly Mock<ILogger<VipAuthorizationService>> _logger;
        private readonly VipAuthorizationService _service;

        public VipAuthorizationServiceTests()
        {
            _userRepo = new Mock<IUserRepository>();
            _logger = new Mock<ILogger<VipAuthorizationService>>();
            _service = new VipAuthorizationService(_userRepo.Object, _logger.Object);
        }

        [Fact]
        public void IsUserVip_UserNotFound_ReturnsFalse()
        {
            _userRepo.Setup(r => r.GetById(1)).Returns((User?)null);
            _service.IsUserVip(1).Should().BeFalse();
        }

        [Fact]
        public void IsUserVip_Expired_ReturnsFalse()
        {
            _userRepo.Setup(r => r.GetById(2)).Returns(new User { VipExpireAt = DateTime.UtcNow.AddDays(-1) });
            _service.IsUserVip(2).Should().BeFalse();
        }

        [Fact]
        public void IsUserVip_Active_ReturnsTrue()
        {
            _userRepo.Setup(r => r.GetById(3)).Returns(new User { VipExpireAt = DateTime.UtcNow.AddDays(5) });
            _service.IsUserVip(3).Should().BeTrue();
        }

        [Fact]
        public void GetVipExpireDate_NotFoundOrNull_ReturnsMin()
        {
            _userRepo.Setup(r => r.GetById(4)).Returns((User?)null);
            _service.GetVipExpireDate(4).Should().Be(DateTime.MinValue);

            _userRepo.Setup(r => r.GetById(5)).Returns(new User { VipExpireAt = null });
            _service.GetVipExpireDate(5).Should().Be(DateTime.MinValue);
        }

        [Fact]
        public void GetVipExpireDate_ReturnsDate()
        {
            var dt = DateTime.UtcNow.AddDays(2);
            _userRepo.Setup(r => r.GetById(6)).Returns(new User { VipExpireAt = dt });
            _service.GetVipExpireDate(6).Should().Be(dt);
        }

        [Fact]
        public void EnsureVipAccess_NotVip_Throws()
        {
            _userRepo.Setup(r => r.GetById(7)).Returns(new User { VipExpireAt = DateTime.UtcNow.AddDays(-1) });
            Action act = () => _service.EnsureVipAccess(7, "feature");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        [Fact]
        public void GetVipStatus_NotFound_ReturnsMessage()
        {
            _userRepo.Setup(r => r.GetById(8)).Returns((User?)null);
            var status = _service.GetVipStatus(8);
            status.IsVip.Should().BeFalse();
            status.Message.Should().Be("User not found");
        }

        [Fact]
        public void GetVipStatus_Expired_ReturnsExpiredMessage()
        {
            _userRepo.Setup(r => r.GetById(9)).Returns(new User { VipExpireAt = DateTime.UtcNow.AddDays(-1) });
            var status = _service.GetVipStatus(9);
            status.IsVip.Should().BeFalse();
            status.Message.Should().Contain("expired");
        }

        [Fact]
        public void GetVipStatus_Active_ReturnsActiveMessage()
        {
            _userRepo.Setup(r => r.GetById(10)).Returns(new User { VipExpireAt = DateTime.UtcNow.AddDays(3) });
            var status = _service.GetVipStatus(10);
            status.IsVip.Should().BeTrue();
            status.DaysRemaining.Should().BeGreaterThan(0);
        }
    }
}



