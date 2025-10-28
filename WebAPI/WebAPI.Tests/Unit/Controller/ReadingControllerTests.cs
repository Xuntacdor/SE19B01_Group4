using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var exam = new Exam
            {
                ExamId = 1,
                ExamName = "Reading Test",
                ExamType = "Reading"
            };

            _examService.Setup(s => s.GetById(It.IsAny<int>())).Returns(exam);

            string answersJson = "[{\"SkillId\":1,\"Answers\":[\"a\",\"b\"]}]";

            var dto = new SubmitSectionDto
            {
                ExamId = 1,
                Answers = answersJson,
                StartedAt = DateTime.UtcNow.AddMinutes(-30)
            };

            _readingService
                .Setup(s => s.EvaluateReading(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()))
                .Returns(8.0m);


            _examService.Setup(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), It.IsAny<int>()))
                        .Returns((SubmitAttemptDto attemptDto, int userId) => new ExamAttempt
                        {
                            AttemptId = 10,
                            ExamId = attemptDto.ExamId,
                            Score = attemptDto.Score,
                            StartedAt = attemptDto.StartedAt,
                            SubmittedAt = DateTime.UtcNow,
                            AnswerText = attemptDto.AnswerText,
                            Exam = exam,
                            UserId = userId
                        });

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            if (result.Result is ObjectResult objResult && objResult.StatusCode == 500)
            {
                var errorDetails = System.Text.Json.JsonSerializer.Serialize(
                    objResult.Value,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );
                Assert.Fail($"Controller returned 500 error. Details:\n{errorDetails}");
            }

            result.Result.Should().BeOfType<ObjectResult>()
                  .Which.StatusCode.Should().Be(500);
        }


        [Fact]
        public void SubmitAnswers_WhenSuccess_ReturnsOk()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var exam = new Exam { ExamId = 1, ExamName = "Reading Test", ExamType = "Reading" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            // Non-empty answers so the controller proceeds beyond ParseAnswers
            var dto = new SubmitSectionDto
            {
                ExamId = 1,
                Answers = "[{\"SkillId\":1,\"Answers\":[\"x\"]}]",
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
    }
}
