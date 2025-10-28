using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class ExamControllerTests
    {
        private readonly Mock<IExamService> _examService;
        private readonly Mock<IReadingService> _readingService;
        private readonly Mock<IListeningService> _listeningService;
        private readonly Mock<ILogger<ExamController>> _logger;
        private readonly ExamController _controller;

        public ExamControllerTests()
        {
            _examService = new Mock<IExamService>();
            _readingService = new Mock<IReadingService>();
            _listeningService = new Mock<IListeningService>();
            _logger = new Mock<ILogger<ExamController>>();
            _controller = new ExamController(_examService.Object, _readingService.Object, _listeningService.Object);
        }

        private Exam CreateSampleExam(int id = 1)
        {
            return new Exam
            {
                ExamId = id,
                ExamName = "IELTS Reading Test",
                ExamType = "Reading",
                CreatedAt = DateTime.UtcNow,
                Readings = new List<Reading>
                {
                    new Reading
                    {
                        ReadingId = 1,
                        ExamId = id,
                        ReadingContent = "Test content",
                        ReadingQuestion = "Test question",
                        ReadingType = "Markdown",
                        DisplayOrder = 1,
                        CorrectAnswer = "a",
                        QuestionHtml = "<p>Test</p>",
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };
        }

        [Fact]
        public void GetAll_ReturnsOkWithListOfExams()
        {
            // Arrange
            var exams = new List<Exam>
            {
                CreateSampleExam(1),
                CreateSampleExam(2)
            };
            _examService.Setup(s => s.GetAll()).Returns(exams);

            // Act
            var result = _controller.GetAll();

            // Assert
            result.Should().NotBeNull();
            _examService.Verify(s => s.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WhenExamExists_ReturnsOk()
        {
            // Arrange
            var exam = CreateSampleExam();
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            // Act
            var result = _controller.GetById(1);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult!.Value.Should().BeOfType<ExamDto>();
        }

        [Fact]
        public void GetById_WhenExamNotFound_ReturnsNotFound()
        {
            // Arrange
            _examService.Setup(s => s.GetById(It.IsAny<int>())).Returns((Exam?)null);

            // Act
            var result = _controller.GetById(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Create_WhenPayloadIsNull_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Create(null!);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Invalid payload");
        }

        [Fact]
        public void Create_WhenServiceReturnsNull_ReturnsBadRequest()
        {
            // Arrange
            var dto = new CreateExamDto { ExamName = "Test", ExamType = "Reading" };
            _examService.Setup(s => s.Create(dto)).Returns((Exam?)null);

            // Act
            var result = _controller.Create(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Failed to create exam");
        }

        [Fact]
        public void Create_WhenSuccessful_ReturnsCreatedAt()
        {
            // Arrange
            var dto = new CreateExamDto { ExamName = "New Test", ExamType = "Reading" };
            var createdExam = CreateSampleExam();
            _examService.Setup(s => s.Create(dto)).Returns(createdExam);

            // Act
            var result = _controller.Create(dto);

            // Assert
            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdAtResult = result.Result as CreatedAtActionResult;
            createdAtResult.Should().NotBeNull();
            createdAtResult!.ActionName.Should().Be(nameof(ExamController.GetById));
        }

        [Fact]
        public void Update_WhenPayloadIsNull_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Update(1, null!);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Update_WhenExamNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new UpdateExamDto { ExamName = "Updated Name" };
            _examService.Setup(s => s.Update(1, dto)).Returns((Exam?)null);

            // Act
            var result = _controller.Update(1, dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Update_WhenSuccessful_ReturnsOk()
        {
            // Arrange
            var dto = new UpdateExamDto { ExamName = "Updated Name" };
            var updatedExam = CreateSampleExam();
            _examService.Setup(s => s.Update(1, dto)).Returns(updatedExam);

            // Act
            var result = _controller.Update(1, dto);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeOfType<ExamDto>();
        }

        [Fact]
        public void Delete_WhenExamNotFound_ReturnsNotFound()
        {
            // Arrange
            _examService.Setup(s => s.Delete(999)).Returns(false);

            // Act
            var result = _controller.Delete(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Delete_WhenSuccessful_ReturnsNoContent()
        {
            // Arrange
            _examService.Setup(s => s.Delete(1)).Returns(true);

            // Act
            var result = _controller.Delete(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _examService.Verify(s => s.Delete(1), Times.Once);
        }

        [Fact]
        public void GetExamAttemptsByUser_ReturnsOkWithAttempts()
        {
            // Arrange
            var attempts = new List<ExamAttemptSummaryDto>
            {
                new ExamAttemptSummaryDto
                {
                    AttemptId = 1,
                    ExamId = 1,
                    TotalScore = 7.5m,
                    SubmittedAt = DateTime.UtcNow,
                    ExamName = "Test Exam",
                    ExamType = "Reading"
                }
            };
            _examService.Setup(s => s.GetExamAttemptsByUser(1)).Returns(attempts);

            // Act
            var result = _controller.GetExamAttemptsByUser(1);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(attempts);
        }

        [Fact]
        public void GetExamAttemptDetail_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _examService.Setup(s => s.GetExamAttemptDetail(999)).Returns((ExamAttemptDto?)null);

            // Act
            var result = _controller.GetExamAttemptDetail(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GetExamAttemptDetail_WhenFound_ReturnsOk()
        {
            // Arrange
            var attempt = new ExamAttemptDto
            {
                AttemptId = 1,
                ExamId = 1,
                TotalScore = 7.5m,
                SubmittedAt = DateTime.UtcNow,
                ExamName = "Test Exam",
                ExamType = "Reading",
                AnswerText = "Test answers"
            };
            _examService.Setup(s => s.GetExamAttemptDetail(1)).Returns(attempt);

            // Act
            var result = _controller.GetExamAttemptDetail(1);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(attempt);
        }
        [Fact]
        public void ConvertToDto_WhenAllNestedCollectionsNull_ReturnsValidExamDto()
        {
            // Arrange
            var exam = new Exam
            {
                ExamId = 1,
                ExamName = "Empty Exam",
                ExamType = "Speaking",
                CreatedAt = DateTime.UtcNow,
                Readings = null,
                Listenings = null,
                Speakings = null,
                Writings = null
            };

            // Act
            var method = typeof(ExamController)
                .GetMethod("ConvertToDto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dto = (ExamDto)method.Invoke(null, new object[] { exam })!;

            // Assert
            dto.Should().NotBeNull();
            dto.Readings.Should().BeEmpty();
            dto.Listenings.Should().BeEmpty();
            dto.Speakings.Should().BeEmpty();
            dto.Writings.Should().BeEmpty();
        }

        [Fact]
        public void ConvertToDto_WhenCollectionsHaveItems_MapsCorrectly()
        {
            // Arrange
            var exam = new Exam
            {
                ExamId = 2,
                ExamName = "Full Exam",
                ExamType = "Mixed",
                CreatedAt = DateTime.UtcNow,
                Readings = new List<Reading> { new Reading { ReadingId = 1, ExamId = 2, ReadingQuestion = "Q" } },
                Listenings = new List<Listening> { new Listening { ListeningId = 1, ExamId = 2, ListeningQuestion = "L" } },
                Speakings = new List<Speaking> { new Speaking { SpeakingId = 1, ExamId = 2, SpeakingQuestion = "S" } },
                Writings = new List<Writing> { new Writing { WritingId = 1, ExamId = 2, WritingQuestion = "W" } }
            };

            // Act
            var method = typeof(ExamController)
                .GetMethod("ConvertToDto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var dto = (ExamDto)method.Invoke(null, new object[] { exam })!;

            // Assert
            dto.Readings.Should().HaveCount(1);
            dto.Listenings.Should().HaveCount(1);
            dto.Speakings.Should().HaveCount(1);
            dto.Writings.Should().HaveCount(1);
        }

        [Fact]
        public void Create_WhenExamNameEmpty_ReturnsBadRequest()
        {
            // Arrange
            var dto = new CreateExamDto { ExamName = "", ExamType = "Reading" };
            _examService.Setup(s => s.Create(dto)).Returns((Exam?)null);

            // Act
            var result = _controller.Create(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

    }
}

