using System.Collections.Generic;
using FluentAssertions;
using Moq;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class AdminServiceTests
    {
        private readonly Mock<IAdminRepository> _repoMock;
        private readonly AdminService _service;

        public AdminServiceTests()
        {
            _repoMock = new Mock<IAdminRepository>();
            _service = new AdminService(_repoMock.Object);
        }

        // ------------------------------------------------------------
        // TEST 1️⃣: GetDashboardStats
        // ------------------------------------------------------------
        [Fact]
        public void GetDashboardStats_ShouldReturnAllCountsFromRepository()
        {
            // Arrange
            _repoMock.Setup(r => r.CountUsers()).Returns(100);
            _repoMock.Setup(r => r.CountExams()).Returns(50);
            _repoMock.Setup(r => r.GetTotalPaidTransactions()).Returns(1234.56m);
            _repoMock.Setup(r => r.CountExamAttempts()).Returns(200);

            // Act
            var result = _service.GetDashboardStats();

            // Assert
            result.totalUsers.Should().Be(100);
            result.totalExams.Should().Be(50);
            result.totalTransactions.Should().Be(1234.56m);
            result.totalAttempts.Should().Be(200);

            _repoMock.Verify(r => r.CountUsers(), Times.Once);
            _repoMock.Verify(r => r.CountExams(), Times.Once);
            _repoMock.Verify(r => r.GetTotalPaidTransactions(), Times.Once);
            _repoMock.Verify(r => r.CountExamAttempts(), Times.Once);
        }

        // ------------------------------------------------------------
        // TEST 2️⃣: GetSalesTrend
        // ------------------------------------------------------------
        [Fact]
        public void GetSalesTrend_ShouldReturnMonthlySalesTrend()
        {
            // Arrange
            var fakeTrend = new List<object>
            {
                new { Month = "January", Total = 1000m },
                new { Month = "February", Total = 1500m }
            };

            _repoMock.Setup(r => r.GetMonthlySalesTrend()).Returns(fakeTrend);

            // Act
            var result = _service.GetSalesTrend();

            // Assert
            result.Should().BeEquivalentTo(fakeTrend);
            _repoMock.Verify(r => r.GetMonthlySalesTrend(), Times.Once);
        }

        // ------------------------------------------------------------
        // TEST 3️⃣: GetDashboardStats_ShouldWork_WhenZeroValues
        // ------------------------------------------------------------
        [Fact]
        public void GetDashboardStats_ShouldHandleZeroValues()
        {
            // Arrange
            _repoMock.Setup(r => r.CountUsers()).Returns(0);
            _repoMock.Setup(r => r.CountExams()).Returns(0);
            _repoMock.Setup(r => r.GetTotalPaidTransactions()).Returns(0m);
            _repoMock.Setup(r => r.CountExamAttempts()).Returns(0);

            // Act
            var result = _service.GetDashboardStats();

            // Assert
            result.totalUsers.Should().Be(0);
            result.totalExams.Should().Be(0);
            result.totalTransactions.Should().Be(0m);
            result.totalAttempts.Should().Be(0);
        }
    }
}
