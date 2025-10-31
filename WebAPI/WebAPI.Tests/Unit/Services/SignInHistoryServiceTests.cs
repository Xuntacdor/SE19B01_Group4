using Moq;
using System;
using System.Collections.Generic;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class SignInHistoryServiceTests
    {
        [Fact]
        public void LogSignIn_ShouldAddSignInHistoryAndSaveChanges()
        {
            // Arrange
            var mockRepo = new Mock<IUserSignInHistoryRepository>();
            var service = new SignInHistoryService(mockRepo.Object);
            int userId = 1;
            string ip = "1.2.3.4";
            string device = "Firefox on Win";

            // Act
            service.LogSignIn(userId, ip, device);

            // Assert
            mockRepo.Verify(x => x.Add(It.Is<UserSignInHistory>(h =>
                h.UserId == userId && h.IpAddress == ip && h.DeviceInfo == device)), Times.Once);
            mockRepo.Verify(x => x.SaveChanges(), Times.Once);
        }

        [Fact]
        public void GetUserHistory_ShouldReturnFromRepository()
        {
            // Arrange
            var histories = new List<UserSignInHistory> { new UserSignInHistory { UserId = 2 } };
            var mockRepo = new Mock<IUserSignInHistoryRepository>();
            mockRepo.Setup(x => x.GetUserHistory(2, 30)).Returns(histories);
            var service = new SignInHistoryService(mockRepo.Object);

            // Act
            var result = service.GetUserHistory(2);

            // Assert
            Assert.Equal(histories, result);
        }
    }
}