using System.Collections.Generic;
using FluentAssertions;
using Moq;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class WritingFeedbackServiceTests
    {
        private readonly Mock<IWritingFeedbackRepository> _repoMock;
        private readonly WritingFeedbackService _service;

        public WritingFeedbackServiceTests()
        {
            _repoMock = new Mock<IWritingFeedbackRepository>();
            _service = new WritingFeedbackService(_repoMock.Object);
        }

        [Fact]
        public void GetByExamAndUser_CallsRepository_AndReturnsList()
        {
            var feedbacks = new List<WritingFeedback>
            {
                new WritingFeedback { FeedbackId = 1, AttemptId = 10, WritingId = 3 },
                new WritingFeedback { FeedbackId = 2, AttemptId = 10, WritingId = 4 }
            };

            _repoMock.Setup(r => r.GetByExamAndUser(10, 5)).Returns(feedbacks);

            var result = _service.GetByExamAndUser(10, 5);

            result.Should().BeEquivalentTo(feedbacks);
            _repoMock.Verify(r => r.GetByExamAndUser(10, 5), Times.Once);
        }

        [Fact]
        public void GetById_CallsRepository_AndReturnsEntity()
        {
            var feedback = new WritingFeedback { FeedbackId = 1, AttemptId = 10, WritingId = 3 };
            _repoMock.Setup(r => r.GetById(1)).Returns(feedback);

            var result = _service.GetById(1);

            result.Should().Be(feedback);
            _repoMock.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenNotFound()
        {
            _repoMock.Setup(r => r.GetById(99)).Returns((WritingFeedback)null);

            var result = _service.GetById(99);

            result.Should().BeNull();
            _repoMock.Verify(r => r.GetById(99), Times.Once);
        }
    }
}
