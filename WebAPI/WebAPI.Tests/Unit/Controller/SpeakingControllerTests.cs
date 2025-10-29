using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controllers
{
    public class SpeakingControllerTests
    {
        private readonly Mock<ISpeakingService> _speakingService;
        private readonly Mock<ISpeakingFeedbackService> _feedbackService;
        private readonly Mock<ISpeechToTextService> _speechService;
        private readonly Mock<ILogger<SpeakingController>> _logger;
        private readonly SpeakingController _controller;

        public SpeakingControllerTests()
        {
            _speakingService = new Mock<ISpeakingService>();
            _feedbackService = new Mock<ISpeakingFeedbackService>();
            _speechService = new Mock<ISpeechToTextService>();
            _logger = new Mock<ILogger<SpeakingController>>();

            _controller = new SpeakingController(
                _speakingService.Object,
                _feedbackService.Object,
                _speechService.Object,
                _logger.Object
            );
        }

        // ======= CREATE =======
        [Fact]
        public void Create_ShouldReturnCreatedAtAction_WhenValid()
        {
            var dto = new SpeakingDTO { SpeakingId = 1, SpeakingQuestion = "Q" };
            _speakingService.Setup(s => s.Create(dto)).Returns(dto);

            var result = _controller.Create(dto);

            result.Should().BeOfType<CreatedAtActionResult>();
            var created = result as CreatedAtActionResult;
            created!.Value.Should().Be(dto);
        }

        [Fact]
        public void Create_ShouldReturnBadRequest_WhenDtoNull()
        {
            var result = _controller.Create(null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Create_ShouldReturnBadRequest_WhenServiceReturnsNull()
        {
            var dto = new SpeakingDTO();
            _speakingService.Setup(s => s.Create(dto)).Returns((SpeakingDTO)null!);

            var result = _controller.Create(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ======= GET =======
        [Fact]
        public void GetById_ShouldReturnOk_WhenFound()
        {
            var dto = new SpeakingDTO { SpeakingId = 1 };
            _speakingService.Setup(s => s.GetById(1)).Returns(dto);

            var result = _controller.GetById(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().Be(dto);
        }

        [Fact]
        public void GetById_ShouldReturnNotFound_WhenNull()
        {
            _speakingService.Setup(s => s.GetById(It.IsAny<int>())).Returns((SpeakingDTO)null!);
            var result = _controller.GetById(99);
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GetByExam_ShouldReturnOkList()
        {
            var list = new List<SpeakingDTO> { new() { SpeakingId = 1 } };
            _speakingService.Setup(s => s.GetByExam(1)).Returns(list);

            var result = _controller.GetByExam(1);
            var ok = Assert.IsType<OkObjectResult>(result);
            ok.Value.Should().BeAssignableTo<IEnumerable<SpeakingDTO>>();
        }

        // ======= UPDATE & DELETE =======
        [Fact]
        public void Update_ShouldReturnOk_WhenFound()
        {
            var dto = new SpeakingDTO { SpeakingQuestion = "Updated" };
            _speakingService.Setup(s => s.Update(1, dto)).Returns(dto);

            var result = _controller.Update(1, dto);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Update_ShouldReturnNotFound_WhenNull()
        {
            var dto = new SpeakingDTO();
            _speakingService.Setup(s => s.Update(1, dto)).Returns((SpeakingDTO)null!);

            var result = _controller.Update(1, dto);
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Delete_ShouldReturnNoContent_WhenDeleted()
        {
            _speakingService.Setup(s => s.Delete(1)).Returns(true);
            _controller.Delete(1).Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Delete_ShouldReturnNotFound_WhenNotDeleted()
        {
            _speakingService.Setup(s => s.Delete(1)).Returns(false);
            _controller.Delete(1).Should().BeOfType<NotFoundResult>();
        }

        // ======= FEEDBACK =======
        [Fact]
        public void GetFeedbackByExam_ShouldReturnOk_WhenDataExists()
        {
            var list = new List<SpeakingFeedback>
            {
                new()
                {
                    Overall = 7.5m,
                    SpeakingAttempt = new SpeakingAttempt { SpeakingId = 1, AudioUrl = "a", Transcript = "b" }
                }
            };
            _feedbackService.Setup(f => f.GetByExamAndUser(1, 1)).Returns(list);

            var result = _controller.GetFeedbackByExam(1, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetFeedbackByExam_ShouldReturnNotFound_WhenEmpty()
        {
            _feedbackService.Setup(f => f.GetByExamAndUser(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<SpeakingFeedback>());

            var result = _controller.GetFeedbackByExam(1, 1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetFeedbackByExam_ShouldReturnNotFound_WhenNull()
        {
            _feedbackService.Setup(f => f.GetByExamAndUser(It.IsAny<int>(), It.IsAny<int>()))
                .Returns((List<SpeakingFeedback>)null!);

            var result = _controller.GetFeedbackByExam(1, 1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetFeedbackBySpeaking_ShouldReturnOk_WhenFound()
        {
            var feedback = new SpeakingFeedback
            {
                Overall = 8,
                SpeakingAttempt = new SpeakingAttempt
                {
                    SpeakingId = 1,
                    AudioUrl = "a",
                    Transcript = "b",
                    ExamAttempt = new ExamAttempt { ExamId = 5 }
                }
            };
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(1, 1)).Returns(feedback);

            _controller.GetFeedbackBySpeaking(1, 1).Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetFeedbackBySpeaking_ShouldReturnNotFound_WhenMissing()
        {
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(1, 1))
                .Returns((SpeakingFeedback)null!);

            _controller.GetFeedbackBySpeaking(1, 1).Should().BeOfType<NotFoundObjectResult>();
        }

        // ======= TRANSCRIBE =======
        [Fact]
        public void Transcribe_ShouldReturnOk_WhenValid()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "x" };
            _speechService.Setup(s => s.TranscribeAndSave(1, "x")).Returns("Hello");

            var result = _controller.Transcribe(dto);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Transcribe_ShouldReturnBadRequest_WhenDtoNull()
        {
            _controller.Transcribe(null!).Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_ShouldReturnBadRequest_WhenAudioMissing()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "" };
            _controller.Transcribe(dto).Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_ShouldReturnBadRequest_WhenAttemptInvalid()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 0, AudioUrl = "x" };
            _controller.Transcribe(dto).Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_ShouldReturn500_WhenExceptionThrown()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "x" };
            _speechService.Setup(s => s.TranscribeAndSave(1, "x")).Throws(new Exception("Boom"));

            var result = _controller.Transcribe(dto);
            var obj = Assert.IsType<ObjectResult>(result);
            obj.StatusCode.Should().Be(500);
        }

        // ======= GRADE SPEAKING =======
        [Fact]
        public void GradeSpeaking_ShouldReturnBadRequest_WhenInvalid()
        {
            _controller.GradeSpeaking(null!).Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GradeSpeaking_ShouldReturnBadRequest_WhenAnswersEmpty()
        {
            var dto = new SpeakingGradeRequestDTO { ExamId = 1, Answers = new List<SpeakingAnswerDTO>() };
            _controller.GradeSpeaking(dto).Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GradeSpeaking_ShouldReturn401_WhenNoUserClaim()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO> { new() { AudioUrl = "a" } }
            };

            var result = _controller.GradeSpeaking(dto);
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public void GradeSpeaking_ShouldReturnOk_WhenSuccess()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO> { new() { AudioUrl = "a", Transcript = "t" } }
            };

            var json = JsonDocument.Parse("{\"ok\":true}");
            _speakingService.Setup(s => s.GradeSpeaking(dto, It.IsAny<int>())).Returns(json);

            var context = new DefaultHttpContext();
            var claims = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("UserId", "1")
            }, "mock");
            context.User = new System.Security.Claims.ClaimsPrincipal(claims);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            var result = _controller.GradeSpeaking(dto);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GradeSpeaking_ShouldReturn500_WhenExceptionThrown()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO> { new() { AudioUrl = "a", Transcript = "t" } }
            };

            _speakingService.Setup(s => s.GradeSpeaking(dto, It.IsAny<int>())).Throws(new Exception("Fail"));

            var context = new DefaultHttpContext();
            var claims = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("UserId", "1")
            }, "mock");
            context.User = new System.Security.Claims.ClaimsPrincipal(claims);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            var result = _controller.GradeSpeaking(dto);
            var obj = Assert.IsType<ObjectResult>(result);
            obj.StatusCode.Should().Be(500);
        }

    }
}
