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

    public class ListeningControllerTests
    {
        private readonly Mock<IListeningService> _listeningService;
        private readonly Mock<IExamService> _examService;
        private readonly Mock<ILogger<ListeningController>> _logger;
        private readonly ListeningController _controller;

        public ListeningControllerTests()
        {
            _listeningService = new Mock<IListeningService>();
            _examService = new Mock<IExamService>();
            _logger = new Mock<ILogger<ListeningController>>();
            _controller = new ListeningController(_listeningService.Object, _examService.Object);
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
        public void GetAll_ReturnsOkWithListenings()
        {
            var dtos = new List<ListeningDto>
            {
                new ListeningDto
                {
                    ListeningId = 1, ExamId = 1, ListeningContent = "Content1",
                    ListeningQuestion = "Q1", ListeningType = "Markdown", DisplayOrder = 1,
                    CorrectAnswer = "a", QuestionHtml = "<p>Q1</p>", CreatedAt = DateTime.UtcNow
                },
                new ListeningDto
                {
                    ListeningId = 2, ExamId = 1, ListeningContent = "Content2",
                    ListeningQuestion = "Q2", ListeningType = "Markdown", DisplayOrder = 2,
                    CorrectAnswer = "b", QuestionHtml = "<p>Q2</p>", CreatedAt = DateTime.UtcNow
                }
            };
            _listeningService.Setup(s => s.GetAll()).Returns(dtos);

            ActionResult<IEnumerable<ListeningDto>> result = _controller.GetAll();

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(dtos);
            _listeningService.Verify(s => s.GetAll(), Times.Once);
        }

        [Fact]
        public void GetById_WhenFound_ReturnsOk()
        {
            var dto = new ListeningDto
            {
                ListeningId = 1,
                ExamId = 1,
                ListeningContent = "Content",
                ListeningQuestion = "Question",
                ListeningType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>",
                CreatedAt = DateTime.UtcNow
            };
            _listeningService.Setup(s => s.GetById(1)).Returns(dto);

            ActionResult<ListeningDto> result = _controller.GetById(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(dto);
        }

        [Fact]
        public void GetById_WhenNotFound_ReturnsNotFound()
        {
            _listeningService.Setup(s => s.GetById(It.IsAny<int>())).Returns((ListeningDto?)null);

            ActionResult<ListeningDto> result = _controller.GetById(42);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GetByExam_ReturnsMappedListenings()
        {
            int examId = 1;
            var listenings = new List<Listening>
            {
                new Listening
                {
                    ListeningId = 1, ExamId = examId, ListeningContent = "Content1",
                    ListeningQuestion = "Q1", ListeningType = "Markdown", DisplayOrder = 1,
                    CorrectAnswer = "a", QuestionHtml = "<p>Q1</p>", CreatedAt = DateTime.UtcNow
                },
                new Listening
                {
                    ListeningId = 2, ExamId = examId, ListeningContent = "Content2",
                    ListeningQuestion = "Q2", ListeningType = "Markdown", DisplayOrder = 2,
                    CorrectAnswer = "b", QuestionHtml = "<p>Q2</p>", CreatedAt = DateTime.UtcNow
                }
            };
            _listeningService.Setup(s => s.GetListeningsByExam(examId)).Returns(listenings);

            ActionResult<IEnumerable<ListeningDto>> result = _controller.GetByExam(examId);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            var dtos = ok!.Value as IEnumerable<ListeningDto>;
            dtos.Should().NotBeNull();
            dtos!.Count().Should().Be(2);
            dtos.Select(d => d.ListeningId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void Add_WhenDtoIsNull_ReturnsBadRequest()
        {
            ActionResult<ListeningDto> result = _controller.Add(null!);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result.Result as BadRequestObjectResult;
            bad!.Value.Should().Be("Invalid data.");
        }

        [Fact]
        public void Add_WhenServiceFails_ReturnsServerError()
        {
            var dto = new CreateListeningDto
            {
                ExamId = 1,
                ListeningContent = "Content",
                ListeningQuestion = "Question",
                ListeningType = null,
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>"
            };
            _listeningService.Setup(s => s.Add(dto)).Returns((ListeningDto?)null);

            ActionResult<ListeningDto> result = _controller.Add(dto);

            result.Result.Should().BeOfType<ObjectResult>();
            var obj = result.Result as ObjectResult;
            obj!.StatusCode.Should().Be(500);
        }

        [Fact]
        public void Add_WhenSuccessful_ReturnsCreatedAt()
        {
            var dto = new CreateListeningDto
            {
                ExamId = 1,
                ListeningContent = "Content",
                ListeningQuestion = "Question",
                ListeningType = null,
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>"
            };
            var created = new ListeningDto
            {
                ListeningId = 10,
                ExamId = 1,
                ListeningContent = "Content",
                ListeningQuestion = "Question",
                ListeningType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = "a",
                QuestionHtml = "<p>Q</p>",
                CreatedAt = DateTime.UtcNow
            };
            _listeningService.Setup(s => s.Add(dto)).Returns(created);

            ActionResult<ListeningDto> result = _controller.Add(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            createdResult!.ActionName.Should().Be(nameof(ListeningController.GetById));
            createdResult.RouteValues!["id"].Should().Be(created.ListeningId);
            createdResult.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public void Update_WhenDtoIsNull_ReturnsBadRequest()
        {
            IActionResult result = _controller.Update(1, null!);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Invalid Listening data.");
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNotFound()
        {
            var dto = new UpdateListeningDto { ListeningContent = "Updated" };
            _listeningService.Setup(s => s.Update(1, dto)).Returns(false);

            IActionResult result = _controller.Update(1, dto);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Listening not found.");
        }

        [Fact]
        public void Update_WhenSuccessful_ReturnsNoContent()
        {
            var dto = new UpdateListeningDto { ListeningContent = "Updated" };
            _listeningService.Setup(s => s.Update(1, dto)).Returns(true);

            IActionResult result = _controller.Update(1, dto);

            result.Should().BeOfType<NoContentResult>();
            _listeningService.Verify(s => s.Update(1, dto), Times.Once);
        }

        [Fact]
        public void Delete_WhenSuccessful_ReturnsNoContent()
        {
            _listeningService.Setup(s => s.Delete(1)).Returns(true);

            IActionResult result = _controller.Delete(1);

            result.Should().BeOfType<NoContentResult>();
            _listeningService.Verify(s => s.Delete(1), Times.Once);
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsNotFound()
        {
            _listeningService.Setup(s => s.Delete(It.IsAny<int>())).Returns(false);

            IActionResult result = _controller.Delete(5);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Listening not found.");
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
            var exam = new Exam { ExamId = 1, ExamName = "Test", ExamType = "Listening" };
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
                ExamName = "Listening Test",
                ExamType = "Listening"
            };

            _examService.Setup(s => s.GetById(It.IsAny<int>())).Returns(exam);

            string answersJson = "[{\"SkillId\":1,\"Answers\":[\"a\",\"b\"]}]";

            var dto = new SubmitSectionDto
            {
                ExamId = 1,
                Answers = answersJson,
                StartedAt = DateTime.UtcNow.AddMinutes(-30)
            };

            _listeningService
                .Setup(s => s.EvaluateListening(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()))
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
            returnedDto.ExamName.Should().Be("Listening Test");
            returnedDto.ExamType.Should().Be("Listening");
            returnedDto.TotalScore.Should().Be(8.0m);
            returnedDto.AnswerText.Should().NotBeNullOrEmpty();

            _examService.Verify(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), It.IsAny<int>()), Times.Once);
            _listeningService.Verify(
                s => s.EvaluateListening(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()),
                Times.Once
            );
        }

        [Fact]
        public void SubmitAnswers_WhenExceptionThrown_ReturnsServerError()
        {
            var context = CreateHttpContextWithSession(out _, userId: 1);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            var exam = new Exam { ExamId = 1, ExamName = "Test", ExamType = "Listening" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            // Non-empty answers so the controller proceeds beyond ParseAnswers
            var dto = new SubmitSectionDto
            {
                ExamId = 1,
                Answers = "[{\"SkillId\":1,\"Answers\":[\"x\"]}]",
                StartedAt = DateTime.UtcNow
            };

            // Force EvaluateListening to throw to hit the catch block
            _listeningService.Setup(s => s.EvaluateListening(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()))
                           .Throws(new InvalidOperationException("Unexpected failure"));

            ActionResult<ExamAttemptDto> result = _controller.SubmitAnswers(dto);

            // The controller should return a 500 ObjectResult when an exception occurs
            result.Result.Should().BeOfType<ObjectResult>();
            var obj = result.Result as ObjectResult;
            obj!.StatusCode.Should().Be(500);
        }

    }
}
