using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public class ListeningControllerTests
    {
        private readonly Mock<IListeningService> _listeningService;
        private readonly Mock<IExamService> _examService;
        private readonly ListeningController _controller;

        public ListeningControllerTests()
        {
            _listeningService = new Mock<IListeningService>();
            _examService = new Mock<IExamService>();
            _controller = new ListeningController(_listeningService.Object, _examService.Object);
        }

        private DefaultHttpContext CreateHttpContextWithSession(int? userId = null)
        {
            var context = new DefaultHttpContext();
            context.Session = new TestSession();
            if (userId.HasValue)
                context.Session.SetInt32("UserId", userId.Value);
            return context;
        }

        // ------------------ BASIC CRUD TESTS ------------------

        [Fact]
        public void GetAll_ReturnsOkWithListenings()
        {
            var dtos = new List<ListeningDto>
            {
                new() { ListeningId = 1, ExamId = 1, ListeningContent = "C1" },
                new() { ListeningId = 2, ExamId = 2, ListeningContent = "C2" }
            };
            _listeningService.Setup(s => s.GetAll()).Returns(dtos);

            var result = _controller.GetAll();

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_WhenFound_ReturnsOk()
        {
            var dto = new ListeningDto { ListeningId = 1 };
            _listeningService.Setup(s => s.GetById(1)).Returns(dto);

            var result = _controller.GetById(1);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_WhenNotFound_ReturnsNotFound()
        {
            _listeningService.Setup(s => s.GetById(It.IsAny<int>())).Returns((ListeningDto)null!);

            var result = _controller.GetById(5);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Add_WhenDtoIsNull_ReturnsBadRequest()
        {
            var result = _controller.Add(null!);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Add_WhenServiceFails_ReturnsServerError()
        {
            var dto = new CreateListeningDto();
            _listeningService.Setup(s => s.Add(dto)).Returns((ListeningDto)null!);

            var result = _controller.Add(dto);

            result.Result.Should().BeOfType<ObjectResult>()
                  .Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public void Add_WhenSuccess_ReturnsCreatedAt()
        {
            var dto = new CreateListeningDto();
            var created = new ListeningDto { ListeningId = 10 };
            _listeningService.Setup(s => s.Add(dto)).Returns(created);

            var result = _controller.Add(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void Update_WhenDtoIsNull_ReturnsBadRequest()
        {
            var result = _controller.Update(1, null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNotFound()
        {
            var dto = new UpdateListeningDto();
            _listeningService.Setup(s => s.Update(1, dto)).Returns(false);

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Update_WhenSuccess_ReturnsNoContent()
        {
            var dto = new UpdateListeningDto();
            _listeningService.Setup(s => s.Update(1, dto)).Returns(true);

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsNotFound()
        {
            _listeningService.Setup(s => s.Delete(5)).Returns(false);
            var result = _controller.Delete(5);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Delete_WhenSuccess_ReturnsNoContent()
        {
            _listeningService.Setup(s => s.Delete(5)).Returns(true);
            var result = _controller.Delete(5);
            result.Should().BeOfType<NoContentResult>();
        }

        // ------------------ SUBMIT ANSWERS TESTS ------------------

        [Fact]
        public void SubmitAnswers_WhenNull_ReturnsBadRequest()
        {
            var result = _controller.SubmitAnswers(null!);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void SubmitAnswers_WhenNoUser_ReturnsUnauthorized()
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

            var exam = new Exam { ExamId = 1, ExamType = "Listening", ExamName = "T" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            var dto = new SubmitSectionDto { ExamId = 1, Answers = "invalid" };

            var result = _controller.SubmitAnswers(dto);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void SubmitAnswers_WhenEvaluateThrows_ReturnsServerError()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };
            var exam = new Exam { ExamId = 1, ExamType = "Listening" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);

            var dto = new SubmitSectionDto { ExamId = 1, Answers = "[{\"SkillId\":1}]" };
            _listeningService.Setup(s => s.EvaluateListening(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()))
                             .Throws(new Exception("fail"));

            var result = _controller.SubmitAnswers(dto);
            result.Result.Should().BeOfType<ObjectResult>()
                  .Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public void SubmitAnswers_WhenSuccessful_ReturnsOk()
        {
            var ctx = CreateHttpContextWithSession(1);
            _controller.ControllerContext = new ControllerContext { HttpContext = ctx };

            var exam = new Exam { ExamId = 1, ExamName = "Listening Test", ExamType = "Listening" };
            _examService.Setup(s => s.GetById(1)).Returns(exam);
            _listeningService.Setup(s => s.EvaluateListening(It.IsAny<int>(), It.IsAny<List<UserAnswerGroup>>()))
                             .Returns(8.0m);

            _examService.Setup(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), 1))
                        .Returns(new ExamAttempt { AttemptId = 10, ExamId = 1, Score = 8, Exam = exam });

            var dto = new SubmitSectionDto { ExamId = 1, Answers = "[{\"SkillId\":1}]" };

            var result = _controller.SubmitAnswers(dto);

            result.Result.Should().BeOfType<OkObjectResult>()
                  .Which.StatusCode.Should().Be(200);
        }
    }
}
