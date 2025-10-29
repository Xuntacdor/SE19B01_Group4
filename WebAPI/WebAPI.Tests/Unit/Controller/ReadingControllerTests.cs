using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;
using WebAPI.Tests.Unit.Controller;
using Xunit;

namespace WebAPI.Tests
{
    public class ReadingControllerTests
    {
        private readonly Mock<IReadingService> _readingService;
        private readonly Mock<IExamService> _examService;
        private readonly ReadingController _controller;

        public ReadingControllerTests()
        {
            _readingService = new Mock<IReadingService>();
            _examService = new Mock<IExamService>();
            _controller = new ReadingController(_readingService.Object, _examService.Object);
        }

        private DefaultHttpContext CreateHttpContextWithSession(int? userId = null)
        {
            var context = new DefaultHttpContext();
            context.Session = new TestSession();
            if (userId.HasValue)
                context.Session.SetInt32("UserId", userId.Value);
            return context;
        }

        [Fact]
        public void GetAll_ReturnsOk()
        {
            var list = new List<ReadingDto> { new() { ReadingId = 1 }, new() { ReadingId = 2 } };
            _readingService.Setup(s => s.GetAll()).Returns(list);

            var result = _controller.GetAll();

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetByExam_ReturnsMappedReadings()
        {
            // Arrange
            var readings = new List<Reading>
            {
                new Reading
                {
                    ReadingId = 1,
                    ExamId = 10,
                    ReadingContent = "Content 1",
                    ReadingQuestion = "Question 1",
                    ReadingType = "Type A",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    CorrectAnswer = "A",
                    QuestionHtml = "<p>Q1</p>"
                },
                new Reading
                {
                    ReadingId = 2,
                    ExamId = 10,
                    ReadingContent = "Content 2",
                    ReadingQuestion = "Question 2",
                    ReadingType = "Type B",
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                    CorrectAnswer = "B",
                    QuestionHtml = "<p>Q2</p>"
                }
            };

            _readingService.Setup(s => s.GetReadingsByExam(10)).Returns(readings);

            // Act
            var result = _controller.GetByExam(10);

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<ReadingDto>>>();
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var dtos = (IEnumerable<ReadingDto>)okResult!.Value!;
            var dtoList = dtos.ToList();
            dtoList.Should().HaveCount(2);

            dtoList[0].ReadingId.Should().Be(1);
            dtoList[0].ExamId.Should().Be(10);
            dtoList[0].ReadingContent.Should().Be("Content 1");
            dtoList[0].ReadingQuestion.Should().Be("Question 1");
            dtoList[0].ReadingType.Should().Be("Type A");
            dtoList[0].DisplayOrder.Should().Be(1);
            dtoList[0].CorrectAnswer.Should().Be("A");
            dtoList[0].QuestionHtml.Should().Be("<p>Q1</p>");

            dtoList[1].ReadingId.Should().Be(2);
        }

        [Fact]
        public void GetById_WhenFound_ReturnsOk()
        {
            var dto = new ReadingDto { ReadingId = 1 };
            _readingService.Setup(s => s.GetById(1)).Returns(dto);

            var result = _controller.GetById(1);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_WhenNotFound_ReturnsNotFound()
        {
            _readingService.Setup(s => s.GetById(It.IsAny<int>())).Returns((ReadingDto)null!);

            var result = _controller.GetById(1);
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Add_WhenNull_ReturnsBadRequest()
        {
            var result = _controller.Add(null!);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Add_WhenServiceFails_Returns500()
        {
            var dto = new CreateReadingDto();
            _readingService.Setup(s => s.Add(dto)).Returns((ReadingDto)null!);

            var result = _controller.Add(dto);
            result.Result.Should().BeOfType<ObjectResult>()
                  .Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public void Add_WhenSuccess_ReturnsCreatedAt()
        {
            var dto = new CreateReadingDto();
            var created = new ReadingDto { ReadingId = 10 };
            _readingService.Setup(s => s.Add(dto)).Returns(created);

            var result = _controller.Add(dto);
            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void Update_WhenNull_ReturnsBadRequest()
        {
            var result = _controller.Update(1, null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNotFound()
        {
            var dto = new UpdateReadingDto();
            _readingService.Setup(s => s.Update(1, dto)).Returns(false);

            var result = _controller.Update(1, dto);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Update_WhenSuccess_ReturnsNoContent()
        {
            var dto = new UpdateReadingDto();
            _readingService.Setup(s => s.Update(1, dto)).Returns(true);

            var result = _controller.Update(1, dto);
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsNotFound()
        {
            _readingService.Setup(s => s.Delete(1)).Returns(false);
            var result = _controller.Delete(1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Delete_WhenSuccess_ReturnsNoContent()
        {
            _readingService.Setup(s => s.Delete(1)).Returns(true);
            var result = _controller.Delete(1);
            result.Should().BeOfType<NoContentResult>();
        }

        // ------------------ SUBMIT ANSWERS ------------------

        [Fact]
        public void SubmitAnswers_WhenDtoNull_ReturnsBadRequest()
        {
            var result = _controller.SubmitAnswers(null!);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void SubmitAnswers_WhenAnswersNull_ReturnsBadRequest()
        {
            var dto = new SubmitSectionDto { ExamId = 1, Answers = null };
            var result = _controller.SubmitAnswers(dto);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void SubmitAnswers_WhenUserNotLoggedIn_ReturnsUnauthorized()
        {
            var ctx = CreateHttpContextWithSession(null);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var dto = new SubmitSectionDto { ExamId = 1, Answers = "[]" };
            var result = _controller.SubmitAnswers(dto);
            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void SubmitAnswers_WhenExamNotFound_ReturnsNotFound()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };
            _examService.Setup(s => s.GetById(1)).Returns((Exam)null!);

            var dto = new SubmitSectionDto { ExamId = 1, Answers = "[]" };
            var result = _controller.SubmitAnswers(dto);
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void SubmitAnswers_WhenParseFails_ReturnsBadRequest()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var exam = new Exam { ExamId = 1, ExamType = "Reading", ExamName = "Test" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);
            var dto = new SubmitSectionDto { ExamId = 1, Answers = "invalid-json", StartedAt = DateTime.UtcNow };

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result.Result as BadRequestObjectResult;
            bad!.Value.Should().Be("No answers found in payload.");
        }
        [Fact]
        public void SubmitAnswers_WhenEvaluateThrows_ReturnsServerError()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var exam = new Exam { ExamId = 1, ExamName = "Reading Test", ExamType = "Reading" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            // Non-empty answers so the controller proceeds beyond ParseAnswers
            var dto = new SubmitSectionDto
            {
                ExamId = 1,
                Answers = "[{\"SkillId\":1,\"Answers\":{\"1_q1\":\"B\"}}]",
                StartedAt = DateTime.UtcNow
            };

            // Force EvaluateReading to throw to hit the catch block
            _readingService.Setup(s => s.EvaluateReading(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()))
                           .Throws(new InvalidOperationException("Unexpected failure"));

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            // The controller should return a 500 ObjectResult when an exception occurs
            result.Result.Should().BeOfType<ObjectResult>();
            var obj = result.Result as ObjectResult;
            obj!.StatusCode.Should().Be(500);
        }

        [Fact]
        public void SubmitAnswers_WhenSuccess_ReturnsOk()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var exam = new Exam { ExamId = 1, ExamName = "Test Reading Exam", ExamType = "Reading" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            var dto = new SubmitSectionDto
            {
                ExamId = 1,
                Answers = "[{\"SkillId\":1,\"Answers\":{\"1_q1\":\"A\"}}]",
                StartedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            var expectedScore = 8.5m;
            _readingService.Setup(s => s.EvaluateReading(1, It.IsAny<List<UserAnswerGroup>>())).Returns(expectedScore);

            var mockAttempt = new ExamAttempt
            {
                AttemptId = 42,
                ExamId = 1,
                UserId = 1,
                AnswerText = JsonSerializer.Serialize(new List<UserAnswerGroup> { new() { SkillId = 1, Answers = new Dictionary<string, object> { ["1_q1"] = "A" } } }),
                Score = expectedScore,
                StartedAt = dto.StartedAt,
                SubmittedAt = DateTime.UtcNow
            };
            _examService.Setup(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), 1)).Returns(mockAttempt);

            // Act
            var result = _controller.SubmitAnswers(dto);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeOfType<ExamAttemptDto>();
            var responseDto = okResult.Value as ExamAttemptDto;

            responseDto.Should().NotBeNull();
            responseDto!.AttemptId.Should().Be(42);
            responseDto.ExamId.Should().Be(1);
            responseDto.ExamName.Should().Be("Test Reading Exam");
            responseDto.ExamType.Should().Be("Reading");
            responseDto.StartedAt.Should().Be(dto.StartedAt);
            responseDto.SubmittedAt.Should().Be(mockAttempt.SubmittedAt);
            responseDto.TotalScore.Should().Be(expectedScore);
            responseDto.AnswerText.Should().NotBeNullOrEmpty();
        }
    }
}
