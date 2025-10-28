using System;
using FluentAssertions;
using Moq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class ExamServiceTests
    {
        private readonly Mock<IExamRepository> _repoMock;
        private readonly Mock<IExamAttemptRepository> _attemptRepoMock;
        private readonly ExamService _service;

        public ExamServiceTests()
        {
            _repoMock = new Mock<IExamRepository>();
            _attemptRepoMock = new Mock<IExamAttemptRepository>();
            _service = new ExamService(_repoMock.Object, _attemptRepoMock.Object);
        }

        private Exam CreateSampleExam(int id = 1)
        {
            return new Exam
            {
                ExamId = id,
                ExamName = "IELTS Reading Test",
                ExamType = "Reading",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public void GetById_WhenExamExists_ReturnsExam()
        {
            // Arrange
            var exam = CreateSampleExam();
            _repoMock.Setup(r => r.GetById(1)).Returns(exam);

            // Act
            var result = _service.GetById(1);

            // Assert
            result.Should().NotBeNull();
            result.ExamId.Should().Be(1);
            _repoMock.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void GetById_WhenExamNotFound_ReturnsNull()
        {
            // Arrange
            _repoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Exam?)null);

            // Act
            var result = _service.GetById(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_ReturnsListOfExams()
        {
            // Arrange
            var exams = new List<Exam> { CreateSampleExam(1), CreateSampleExam(2) };
            _repoMock.Setup(r => r.GetAll()).Returns(exams);

            // Act
            var result = _service.GetAll();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _repoMock.Verify(r => r.GetAll(), Times.Once);
        }

        [Fact]
        public void Create_WithValidDto_ReturnsCreatedExam()
        {
            // Arrange
            var dto = new CreateExamDto { ExamName = "New Test", ExamType = "Reading" };
            var expectedExam = CreateSampleExam();
            _repoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => e.ExamId = 1);
            _repoMock.Setup(r => r.SaveChanges()).Verifiable();

            // Act
            var result = _service.Create(dto);

            // Assert
            result.Should().NotBeNull();
            result.ExamName.Should().Be("New Test");
            result.ExamType.Should().Be("Reading");
            _repoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Update_WhenExamExists_ReturnsUpdatedExam()
        {
            // Arrange
            var existingExam = CreateSampleExam();
            var dto = new UpdateExamDto { ExamName = "Updated Name" };
            _repoMock.Setup(r => r.GetById(1)).Returns(existingExam);
            _repoMock.Setup(r => r.Update(It.IsAny<Exam>())).Verifiable();
            _repoMock.Setup(r => r.SaveChanges()).Verifiable();

            // Act
            var result = _service.Update(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.ExamName.Should().Be("Updated Name");
            _repoMock.Verify(r => r.Update(existingExam), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Update_WhenExamNotFound_ReturnsNull()
        {
            // Arrange
            var dto = new UpdateExamDto { ExamName = "Updated Name" };
            _repoMock.Setup(r => r.GetById(999)).Returns((Exam?)null);

            // Act
            var result = _service.Update(999, dto);

            // Assert
            result.Should().BeNull();
            _repoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Never);
        }

        [Fact]
        public void Update_WhenDtoHasNullValues_OnlyUpdatesNonNullFields()
        {
            // Arrange
            var existingExam = CreateSampleExam();
            var dto = new UpdateExamDto { ExamType = "Listening" };
            _repoMock.Setup(r => r.GetById(1)).Returns(existingExam);
            _repoMock.Setup(r => r.Update(It.IsAny<Exam>())).Verifiable();
            _repoMock.Setup(r => r.SaveChanges()).Verifiable();

            // Act
            var result = _service.Update(1, dto);

            // Assert
            result.Should().NotBeNull();
            result.ExamType.Should().Be("Listening");
            result.ExamName.Should().Be("IELTS Reading Test"); // Unchanged
        }

        [Fact]
        public void Delete_WhenExamExists_ReturnsTrue()
        {
            // Arrange
            var exam = CreateSampleExam();
            _repoMock.Setup(r => r.GetById(1)).Returns(exam);
            _repoMock.Setup(r => r.Delete(exam)).Verifiable();
            _repoMock.Setup(r => r.SaveChanges()).Verifiable();

            // Act
            var result = _service.Delete(1);

            // Assert
            result.Should().BeTrue();
            _repoMock.Verify(r => r.Delete(exam), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_WhenExamNotFound_ReturnsFalse()
        {
            // Arrange
            _repoMock.Setup(r => r.GetById(999)).Returns((Exam?)null);

            // Act
            var result = _service.Delete(999);

            // Assert
            result.Should().BeFalse();
            _repoMock.Verify(r => r.Delete(It.IsAny<Exam>()), Times.Never);
        }

        [Fact]
        public void SubmitAttempt_WithValidDto_ReturnsAttempt()
        {
            // Arrange
            var exam = CreateSampleExam();
            var dto = new SubmitAttemptDto
            {
                ExamId = 1,
                AnswerText = "test answers",
                Score = 7.5m,
                StartedAt = DateTime.UtcNow
            };
            _repoMock.Setup(r => r.GetById(1)).Returns(exam);
            _attemptRepoMock.Setup(r => r.Add(It.IsAny<ExamAttempt>())).Verifiable();
            _attemptRepoMock.Setup(r => r.SaveChanges()).Verifiable();

            // Act
            var result = _service.SubmitAttempt(dto, 1);

            // Assert
            result.Should().NotBeNull();
            result.ExamId.Should().Be(1);
            result.UserId.Should().Be(1);
            result.Score.Should().Be(7.5m);
            _attemptRepoMock.Verify(r => r.Add(It.IsAny<ExamAttempt>()), Times.Once);
            _attemptRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void SubmitAttempt_WhenExamNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dto = new SubmitAttemptDto { ExamId = 999 };
            _repoMock.Setup(r => r.GetById(999)).Returns((Exam?)null);

            // Act
            Action act = () => _service.SubmitAttempt(dto, 1);

            // Assert
            act.Should().Throw<KeyNotFoundException>().WithMessage("Exam not found");
        }

        [Fact]
        public void GetAttemptById_WhenExists_ReturnsAttempt()
        {
            // Arrange
            var attempt = new ExamAttempt
            {
                AttemptId = 1,
                ExamId = 1,
                UserId = 1,
                Score = 7.5m,
                SubmittedAt = DateTime.UtcNow
            };
            _attemptRepoMock.Setup(r => r.GetById(1)).Returns(attempt);

            // Act
            var result = _service.GetAttemptById(1);

            // Assert
            result.Should().NotBeNull();
            result.AttemptId.Should().Be(1);
            _attemptRepoMock.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void Save_CallsSaveChangesOnBothRepositories()
        {
            // Arrange
            _repoMock.Setup(r => r.SaveChanges()).Verifiable();
            _attemptRepoMock.Setup(r => r.SaveChanges()).Verifiable();

            // Act
            _service.Save();

            // Assert
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
            _attemptRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }
    }
}


