using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;
using WebAPI.Models;

namespace WebAPI.Tests
{
    public class WritingControllerTests
    {
        private readonly Mock<IWritingService> _writingServiceMock;
        private readonly Mock<IWritingFeedbackService> _feedbackServiceMock;

        public WritingControllerTests()
        {
            _writingServiceMock = new Mock<IWritingService>();
            _feedbackServiceMock = new Mock<IWritingFeedbackService>();
        }

        private WritingController CreateController(string userId = null)
        {
            var controller = new WritingController(_writingServiceMock.Object, _feedbackServiceMock.Object);
            var httpContext = new DefaultHttpContext();
            if (userId != null)
            {
                var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("UserId", userId) }, "TestAuth"));
                httpContext.User = claims;
            }
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return controller;
        }

        [Fact]
        public void GivenValidId_WhenGetById_ThenReturnsOk()
        {
            var dto = new WritingDTO
            {
                WritingId = 1,
                ExamId = 2,
                WritingQuestion = "Q",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = "img"
            };
            _writingServiceMock.Setup(s => s.GetById(1)).Returns(dto);
            var controller = CreateController();

            var result = controller.GetById(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().Be(dto);
        }


        [Fact]
        public void GivenMissingId_WhenGetById_ThenReturnsNotFound()
        {
            _writingServiceMock.Setup(s => s.GetById(It.IsAny<int>())).Returns((WritingDTO)null);
            var controller = CreateController();

            var result = controller.GetById(1);

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GivenServiceThrows_WhenGetById_ThenThrows()
        {
            _writingServiceMock.Setup(s => s.GetById(1)).Throws(new Exception("error"));
            var controller = CreateController();

            Action act = () => controller.GetById(1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenWritingsExist_WhenGetByExam_ThenReturnsOkWithList()
        {
            var dtos = new List<WritingDTO>
            {
                new WritingDTO { WritingId = 1, ExamId = 1, WritingQuestion = "Q1", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
                new WritingDTO { WritingId = 2, ExamId = 1, WritingQuestion = "Q2", DisplayOrder = 2, CreatedAt = DateTime.UtcNow }
            };
            _writingServiceMock.Setup(s => s.GetByExam(1)).Returns(dtos);
            var controller = CreateController();

            var result = controller.GetByExam(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(dtos);
        }

        [Fact]
        public void GivenNoWritings_WhenGetByExam_ThenReturnsOkWithEmptyList()
        {
            _writingServiceMock.Setup(s => s.GetByExam(It.IsAny<int>())).Returns(new List<WritingDTO>());
            var controller = CreateController();

            var result = controller.GetByExam(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            (ok!.Value as IEnumerable<WritingDTO>)!.Should().BeEmpty();
        }

        [Fact]
        public void GivenServiceThrows_WhenGetByExam_ThenThrows()
        {
            _writingServiceMock.Setup(s => s.GetByExam(It.IsAny<int>())).Throws(new Exception("error"));
            var controller = CreateController();

            Action act = () => controller.GetByExam(1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenValidDto_WhenCreate_ThenReturnsCreatedAtAction()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1, CreatedAt = DateTime.UtcNow };
            var created = new WritingDTO { WritingId = 5, ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1, CreatedAt = dto.CreatedAt };
            _writingServiceMock.Setup(s => s.Create(dto)).Returns(created);
            var controller = CreateController();

            var result = controller.Create(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdAt = result.Result as CreatedAtActionResult;
            createdAt!.ActionName.Should().Be(nameof(WritingController.GetById));
            createdAt.RouteValues!["id"].Should().Be(5);
            createdAt.Value.Should().Be(created);
        }

        [Fact]
        public void GivenNullDto_WhenCreate_ThenReturnsBadRequest()
        {
            var controller = CreateController();

            var result = controller.Create(null);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GivenServiceThrows_WhenCreate_ThenThrows()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1 };
            _writingServiceMock.Setup(s => s.Create(dto)).Throws(new Exception("error"));
            var controller = CreateController();

            Action act = () => controller.Create(dto);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenValidUpdate_WhenUpdate_ThenReturnsOk()
        {
            var dto = new WritingDTO { WritingQuestion = "Updated", DisplayOrder = 2, ImageUrl = "img" };
            var updated = new WritingDTO { WritingId = 1, ExamId = 1, WritingQuestion = "Updated", DisplayOrder = 2, CreatedAt = DateTime.UtcNow, ImageUrl = "img" };
            _writingServiceMock.Setup(s => s.Update(1, dto)).Returns(updated);
            var controller = CreateController();

            var result = controller.Update(1, dto);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().Be(updated);
        }

        [Fact]
        public void GivenNotFound_WhenUpdate_ThenReturnsNotFound()
        {
            _writingServiceMock.Setup(s => s.Update(It.IsAny<int>(), It.IsAny<WritingDTO>())).Returns((WritingDTO)null);
            var controller = CreateController();

            var result = controller.Update(1, new WritingDTO { WritingQuestion = "Q", DisplayOrder = 1 });

            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GivenServiceThrows_WhenUpdate_ThenThrows()
        {
            var dto = new WritingDTO { WritingQuestion = "Q", DisplayOrder = 1 };
            _writingServiceMock.Setup(s => s.Update(1, dto)).Throws(new Exception("error"));
            var controller = CreateController();

            Action act = () => controller.Update(1, dto);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenDeleteSucceeds_WhenDelete_ThenReturnsNoContent()
        {
            _writingServiceMock.Setup(s => s.Delete(1)).Returns(true);
            var controller = CreateController();

            var result = controller.Delete(1);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void GivenDeleteFails_WhenDelete_ThenReturnsNotFound()
        {
            _writingServiceMock.Setup(s => s.Delete(It.IsAny<int>())).Returns(false);
            var controller = CreateController();

            var result = controller.Delete(1);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GivenServiceThrows_WhenDelete_ThenThrows()
        {
            _writingServiceMock.Setup(s => s.Delete(1)).Throws(new Exception("error"));
            var controller = CreateController();

            Action act = () => controller.Delete(1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenNullDto_WhenGradeWriting_ThenReturnsBadRequest()
        {
            var controller = CreateController("123");

            var result = controller.GradeWriting(null);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GivenNullAnswers_WhenGradeWriting_ThenReturnsBadRequest()
        {
            var controller = CreateController("123");
            var dto = new WritingGradeRequestDTO { Mode = "single", ExamId = 1, Answers = null };

            var result = controller.GradeWriting(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GivenEmptyAnswers_WhenGradeWriting_ThenReturnsBadRequest()
        {
            var controller = CreateController("123");
            var dto = new WritingGradeRequestDTO { Mode = "single", ExamId = 1, Answers = new List<WritingAnswerDTO>() };

            var result = controller.GradeWriting(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GivenNoUser_WhenGradeWriting_ThenReturnsUnauthorized()
        {
            var controller = CreateController();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A1", ImageUrl = null, DisplayOrder = 1 }
                }
            };
            _writingServiceMock.Setup(s => s.GradeWriting(dto, It.IsAny<int>())).Returns(JsonDocument.Parse("{}"));

            var result = controller.GradeWriting(dto);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void GivenServiceThrows_WhenGradeWriting_ThenReturnsServerError()
        {
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A1", ImageUrl = null, DisplayOrder = 1 }
                }
            };
            _writingServiceMock.Setup(s => s.GradeWriting(dto, 123)).Throws(new Exception("error"));
            var controller = CreateController("123");

            var result = controller.GradeWriting(dto);

            result.Should().BeOfType<ObjectResult>();
            var obj = result as ObjectResult;
            obj!.StatusCode.Should().Be(500);
        }

        [Fact]
        public void GivenValidRequest_WhenGradeWriting_ThenReturnsOk()
        {
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A1", ImageUrl = null, DisplayOrder = 1 }
                }
            };
            var json = JsonDocument.Parse("{\"foo\":\"bar\"}");
            _writingServiceMock.Setup(s => s.GradeWriting(dto, 123)).Returns(json);
            var controller = CreateController("123");

            var result = controller.GradeWriting(dto);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            var doc = ok!.Value as JsonDocument;
            doc!.RootElement.GetProperty("foo").GetString().Should().Be("bar");
        }
        [Fact]
        public void GivenFeedbacksExist_WhenGetFeedbackByExam_ThenReturnsOk()
        {
            var examId = 1;
            var userId = 123;
            var feedbacks = new List<WritingFeedback>
    {
        new WritingFeedback { WritingId = 1, Overall = 7, GrammarAccuracy = 6, TaskAchievement = 8, CoherenceCohesion = 7, LexicalResource = 7, CreatedAt = DateTime.UtcNow },
        new WritingFeedback { WritingId = 2, Overall = 8, GrammarAccuracy = 7, TaskAchievement = 8, CoherenceCohesion = 7, LexicalResource = 8, CreatedAt = DateTime.UtcNow }
    };
            _feedbackServiceMock.Setup(f => f.GetByExamAndUser(examId, userId)).Returns(feedbacks);

            var controller = CreateController("123");

            var result = controller.GetFeedbackByExam(examId, userId);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;

            // ✅ convert object thành JSON text rồi parse lại để dễ check
            var json = JsonSerializer.Serialize(ok!.Value);
            var doc = JsonDocument.Parse(json);

            doc.RootElement.GetProperty("examId").GetInt32().Should().Be(examId);
            doc.RootElement.GetProperty("userId").GetInt32().Should().Be(userId);
            doc.RootElement.GetProperty("totalTasks").GetInt32().Should().Be(2);
        }

        [Fact]
        public void GivenNoFeedbacks_WhenGetFeedbackByExam_ThenReturnsNotFound()
        {
            _feedbackServiceMock.Setup(f => f.GetByExamAndUser(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<WritingFeedback>());

            var controller = CreateController("123");

            var result = controller.GetFeedbackByExam(1, 1);

            result.Should().BeOfType<NotFoundObjectResult>();
        }
        [Fact]
        public void GivenVIPUser_WhenGradeWriting_ThenReturnsOk()
        {
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
        {
            new WritingAnswerDTO { WritingId = 1, AnswerText = "Sample", DisplayOrder = 1 }
        }
            };
            var json = JsonDocument.Parse("{\"success\":true}");
            _writingServiceMock.Setup(s => s.GradeWriting(dto, 123)).Returns(json);
            var controller = CreateController("123");

            var result = controller.GradeWriting(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

    }
}
