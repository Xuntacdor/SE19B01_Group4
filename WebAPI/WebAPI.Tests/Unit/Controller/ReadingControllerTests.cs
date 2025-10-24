using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests
{
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }

    public class SessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    public class ReadingControllerTests
    {
        private readonly Mock<IReadingService> _readingService;
        private readonly Mock<IExamService> _examService;
        private readonly Mock<ILogger<ReadingController>> _logger;
        private readonly ReadingController _controller;

        public ReadingControllerTests()
        {
            _readingService = new Mock<IReadingService>();
            _examService = new Mock<IExamService>();
            _logger = new Mock<ILogger<ReadingController>>();
            _controller = new ReadingController(_readingService.Object, _examService.Object);
        }

        private DefaultHttpContext CreateHttpContextWithSession(out TestSession session, int? userId = null)
        {
            var context = new DefaultHttpContext();
            session = new TestSession();
            context.Session = session;
            if (userId.HasValue)
            {
                var bytes = BitConverter.GetBytes(userId.Value);
                session.Set("UserId", bytes);
            }
            var feature = new SessionFeature { Session = session };
            context.Features.Set<ISessionFeature>(feature);
            return context;
        }

        [Fact]
        public void GetAll_ReturnsOkWithReadings()
        {
            var dtos = new List<ReadingDto>
            {
                new ReadingDto
                {
                    ReadingId = 1, ExamId = 1, ReadingContent = "Content1",
                    ReadingQuestion = "Q1", ReadingType = "Markdown", DisplayOrder = 1,
                    CorrectAnswer = "a", QuestionHtml = "<p>Q1</p>", CreatedAt = DateTime.UtcNow
                },
                new ReadingDto
                {
                    ReadingId = 2, ExamId = 1, ReadingContent = "Content2",
                    ReadingQuestion = "Q2", ReadingType = "Markdown", DisplayOrder = 2,
                    CorrectAnswer = "b", QuestionHtml = "<p>Q2</p>", CreatedAt = DateTime.UtcNow
                }
            };
            _readingService.Setup(s => s.GetAll()).Returns(dtos);

            ActionResult<IEnumerable<ReadingDto>> result = _controller.GetAll();

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(dtos);
            _readingService.Verify(s => s.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WhenFound_ReturnsOk()
        {
            var dto = new ReadingDto
            {
                ReadingId = 1,
                ExamId = 1,
                ReadingContent = "Content",
                ReadingQuestion = "Question",
                ReadingType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>",
                CreatedAt = DateTime.UtcNow
            };
            _readingService.Setup(s => s.GetById(1)).Returns(dto);

            ActionResult<ReadingDto> result = _controller.GetById(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public void GetById_WhenNotFound_ReturnsNotFound()
        {
            _readingService.Setup(s => s.GetById(It.IsAny<int>())).Returns((ReadingDto?)null);

            ActionResult<ReadingDto> result = _controller.GetById(42);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GetByExam_ReturnsMappedReadings()
        {
            int examId = 1;
            var readings = new List<Reading>
            {
                new Reading
                {
                    ReadingId = 1, ExamId = examId, ReadingContent = "Content1",
                    ReadingQuestion = "Q1", ReadingType = "Markdown", DisplayOrder = 1,
                    CorrectAnswer = "a", QuestionHtml = "<p>Q1</p>", CreatedAt = DateTime.UtcNow
                },
                new Reading
                {
                    ReadingId = 2, ExamId = examId, ReadingContent = "Content2",
                    ReadingQuestion = "Q2", ReadingType = "Markdown", DisplayOrder = 2,
                    CorrectAnswer = "b", QuestionHtml = "<p>Q2</p>", CreatedAt = DateTime.UtcNow
                }
            };
            _readingService.Setup(s => s.GetReadingsByExam(examId)).Returns(readings);

            ActionResult<IEnumerable<ReadingDto>> result = _controller.GetByExam(examId);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            var dtos = ok!.Value as IEnumerable<ReadingDto>;
            dtos.Should().NotBeNull();
            dtos!.Count().Should().Be(2);
            dtos.Select(d => d.ReadingId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void Add_WhenDtoIsNull_ReturnsBadRequest()
        {
            ActionResult<ReadingDto> result = _controller.Add(null!);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result.Result as BadRequestObjectResult;
            bad!.Value.Should().Be("Invalid data.");
        }

        [Fact]
        public void Add_WhenServiceFails_ReturnsServerError()
        {
            var dto = new CreateReadingDto
            {
                ExamId = 1,
                ReadingContent = "Content",
                ReadingQuestion = "Question",
                ReadingType = null,
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>"
            };
            _readingService.Setup(s => s.Add(dto)).Returns((ReadingDto?)null);

            ActionResult<ReadingDto> result = _controller.Add(dto);

            result.Result.Should().BeOfType<ObjectResult>();
            var obj = result.Result as ObjectResult;
            obj!.StatusCode.Should().Be(500);
        }

        [Fact]
        public void Add_WhenSuccessful_ReturnsCreatedAt()
        {
            var dto = new CreateReadingDto
            {
                ExamId = 1,
                ReadingContent = "Content",
                ReadingQuestion = "Question",
                ReadingType = null,
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>"
            };
            var created = new ReadingDto
            {
                ReadingId = 10,
                ExamId = 1,
                ReadingContent = "Content",
                ReadingQuestion = "Question",
                ReadingType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>",
                CreatedAt = DateTime.UtcNow
            };
            _readingService.Setup(s => s.Add(dto)).Returns(created);

            ActionResult<ReadingDto> result = _controller.Add(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.ActionName.Should().Be(nameof(ReadingController.GetById));
            createdResult.RouteValues!["id"].Should().Be(created.ReadingId);
            createdResult.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public void Update_WhenDtoIsNull_ReturnsBadRequest()
        {
            IActionResult result = _controller.Update(1, null!);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Invalid reading data.");
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNotFound()
        {
            var dto = new UpdateReadingDto { ReadingContent = "Updated" };
            _readingService.Setup(s => s.Update(1, dto)).Returns(false);

            IActionResult result = _controller.Update(1, dto);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Reading not found.");
        }

        [Fact]
        public void Update_WhenSuccessful_ReturnsNoContent()
        {
            var dto = new UpdateReadingDto { ReadingContent = "Updated" };
            _readingService.Setup(s => s.Update(1, dto)).Returns(true);

            IActionResult result = _controller.Update(1, dto);

            result.Should().BeOfType<NoContentResult>();
            _readingService.Verify(s => s.Update(1, dto), Times.Once);
        }

        [Fact]
        public void Delete_WhenSuccessful_ReturnsNoContent()
        {
            _readingService.Setup(s => s.Delete(1)).Returns(true);

            IActionResult result = _controller.Delete(1);

            result.Should().BeOfType<NoContentResult>();
            _readingService.Verify(s => s.Delete(1), Times.Once);
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsNotFound()
        {
            _readingService.Setup(s => s.Delete(It.IsAny<int>())).Returns(false);

            IActionResult result = _controller.Delete(5);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Reading not found.");
        }

        [Fact]
        public void SubmitAnswers_WhenDtoNull_ReturnsBadRequest()
        {
            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(null!);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result.Result as BadRequestObjectResult;
            bad!.Value.Should().Be("Invalid or empty payload.");
        }

        [Fact]
        public void SubmitAnswers_WhenAnswersNull_ReturnsBadRequest()
        {
            var dto = new SubmitSectionDto { ExamId = 1, Answers = null, StartedAt = DateTime.UtcNow };

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result.Result as BadRequestObjectResult;
            bad!.Value.Should().Be("Invalid or empty payload.");
        }

        [Fact]
        public void SubmitAnswers_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            var context = CreateHttpContextWithSession(out _);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            var dto = new SubmitSectionDto { ExamId = 1, Answers = "[]", StartedAt = DateTime.UtcNow };

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result.Result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Please login to submit exam.");
        }

        [Fact]
        public void SubmitAnswers_WhenExamNotFound_ReturnsNotFound()
        {
            var context = CreateHttpContextWithSession(out var session, userId: 1);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            _examService.Setup(s => s.GetById(It.IsAny<int>())).Returns((Exam?)null);
            var dto = new SubmitSectionDto { ExamId = 1, Answers = "[]", StartedAt = DateTime.UtcNow };

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Exam not found.");
        }

        [Fact]
        public void SubmitAnswers_WhenNoAnswersAfterParse_ReturnsBadRequest()
        {
            var context = CreateHttpContextWithSession(out var session, userId: 1);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            var exam = new Exam { ExamId = 1, ExamName = "Test", ExamType = "Reading" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);
            var dto = new SubmitSectionDto { ExamId = 1, Answers = "invalid-json", StartedAt = DateTime.UtcNow };

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result.Result as BadRequestObjectResult;
            bad!.Value.Should().Be("No answers found in payload.");
        }
        [Fact]
        public void SubmitAnswers_WhenSuccessful_ReturnsOkAttemptDto()
        {
            var context = CreateHttpContextWithSession(out _, userId: 1);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

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

            result.Result.Should().BeAssignableTo<ObjectResult>();
            var ok = result.Result as ObjectResult;
            ok.Should().NotBeNull();
            ok!.StatusCode.Should().Be(200);

            var returnedDto = ok.Value as ExamAttemptDto;
            returnedDto.Should().NotBeNull();
            returnedDto!.AttemptId.Should().Be(10);
            returnedDto.ExamId.Should().Be(1);
            returnedDto.ExamName.Should().Be("Reading Test");
            returnedDto.ExamType.Should().Be("Reading");
            returnedDto.TotalScore.Should().Be(8.0m);
            returnedDto.AnswerText.Should().NotBeNullOrEmpty();

            _examService.Verify(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), It.IsAny<int>()), Times.Once);
            _readingService.Verify(
                s => s.EvaluateReading(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()),
                Times.Once
            );
        }


        [Fact]
        public void SubmitAnswers_WhenExceptionThrown_ReturnsServerError()
        {
            var context = CreateHttpContextWithSession(out _, userId: 1);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            var exam = new Exam { ExamId = 1, ExamName = "Test", ExamType = "Reading" };
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
