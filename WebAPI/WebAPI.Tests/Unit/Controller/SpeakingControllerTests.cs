using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebAPI.Controllers;
using WebAPI.Models;
using WebAPI.DTOs;
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
        private void SetUser(int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("UserId", userId.ToString())
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }
        // ========== CREATE ==========
        [Fact]
        public void Create_NullDto_ReturnsBadRequest()
        {
            var result = _controller.Create(null);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Create_Valid_ReturnsCreated()
        {
            var dto = new SpeakingDTO { SpeakingId = 1, SpeakingQuestion = "Q" };
            _speakingService.Setup(s => s.Create(dto)).Returns(dto);

            var result = _controller.Create(dto).Result as CreatedAtActionResult;
            result.Should().NotBeNull();
            result!.ActionName.Should().Be(nameof(SpeakingController.GetById));
            result.Value.Should().Be(dto);
        }

        // ========== GET BY ID ==========
        [Fact]
        public void GetById_Found_ReturnsOk()
        {
            var dto = new SpeakingDTO { SpeakingId = 1 };
            _speakingService.Setup(s => s.GetById(1)).Returns(dto);

            var result = _controller.GetById(1).Result as OkObjectResult;
            result.Should().NotBeNull();
            result!.Value.Should().Be(dto);
        }

        [Fact]
        public void GetById_NotFound_Returns404()
        {
            _speakingService.Setup(s => s.GetById(It.IsAny<int>())).Returns((SpeakingDTO)null);
            var result = _controller.GetById(99).Result;
            result.Should().BeOfType<NotFoundResult>();
        }

        // ========== GET BY EXAM ==========
        [Fact]
        public void GetByExam_ReturnsOkList()
        {
            var list = new List<SpeakingDTO> { new SpeakingDTO { SpeakingId = 1 } };
            _speakingService.Setup(s => s.GetByExam(1)).Returns(list);

            var result = _controller.GetByExam(1).Result as OkObjectResult;
            result.Should().NotBeNull();
            (result!.Value as IEnumerable<SpeakingDTO>)!.Should().HaveCount(1);
        }

        // ========== UPDATE ==========
        [Fact]
        public void Update_NotFound_Returns404()
        {
            _speakingService.Setup(s => s.Update(1, It.IsAny<SpeakingDTO>())).Returns((SpeakingDTO)null);
            var result = _controller.Update(1, new SpeakingDTO()).Result;
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Update_Found_ReturnsOk()
        {
            var dto = new SpeakingDTO { SpeakingId = 1 };
            _speakingService.Setup(s => s.Update(1, dto)).Returns(dto);

            var result = _controller.Update(1, dto).Result as OkObjectResult;
            result.Should().NotBeNull();
            result!.Value.Should().Be(dto);
        }

        // ========== DELETE ==========
        [Fact]
        public void Delete_Success_ReturnsNoContent()
        {
            _speakingService.Setup(s => s.Delete(1)).Returns(true);
            var result = _controller.Delete(1);
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Delete_NotFound_Returns404()
        {
            _speakingService.Setup(s => s.Delete(2)).Returns(false);
            var result = _controller.Delete(2);
            result.Should().BeOfType<NotFoundResult>();
        }

        // ========== GET FEEDBACK BY EXAM ==========
        [Fact]
        public void GetFeedbackByExam_NoFeedback_Returns404()
        {
            _feedbackService.Setup(f => f.GetByExamAndUser(1, 1))
                .Returns(new List<SpeakingFeedback>());

            var result = _controller.GetFeedbackByExam(1, 1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetFeedbackByExam_ReturnsOk()
        {
            var feedbacks = new List<SpeakingFeedback>
            {
                new SpeakingFeedback
                {
                    SpeakingAttemptId = 11,
                    Overall = 8,
                    SpeakingAttempt = new SpeakingAttempt
                    {
                        SpeakingId = 10,
                        AudioUrl = "url",
                        Transcript = "text"
                    }
                }
            };
            _feedbackService.Setup(f => f.GetByExamAndUser(1, 1))
                .Returns(feedbacks);

            var result = _controller.GetFeedbackByExam(1, 1) as OkObjectResult;
            result.Should().NotBeNull();
        }

        // ========== GET FEEDBACK BY SPEAKING ==========
        [Fact]
        public void GetFeedbackBySpeaking_NotFound_Returns404()
        {
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(1, 1))
                .Returns((SpeakingFeedback)null);

            var result = _controller.GetFeedbackBySpeaking(1, 1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetFeedbackBySpeaking_ReturnsOk()
        {
            var feedback = new SpeakingFeedback
            {
                SpeakingAttemptId = 1,
                Overall = 9,
                SpeakingAttempt = new SpeakingAttempt
                {
                    ExamAttempt = new ExamAttempt { ExamId = 1 },
                    SpeakingId = 2,
                    AudioUrl = "a",
                    Transcript = "b"
                }
            };

            _feedbackService.Setup(f => f.GetBySpeakingAndUser(1, 1)).Returns(feedback);
            var result = _controller.GetFeedbackBySpeaking(1, 1) as OkObjectResult;
            result.Should().NotBeNull();
        }

        // ========== TRANSCRIBE ==========
        [Fact]
        public void Transcribe_NullDto_ReturnsBadRequest()
        {
            var result = _controller.Transcribe(null);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_MissingAudioUrl_ReturnsBadRequest()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "" };
            var result = _controller.Transcribe(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_InvalidAttemptId_ReturnsBadRequest()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 0, AudioUrl = "a" };
            var result = _controller.Transcribe(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_Success_ReturnsOk()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "url" };
            _speechService.Setup(s => s.TranscribeAndSave(It.IsAny<long>(), It.IsAny<string>()))
                .Returns("transcribed");

            var result = _controller.Transcribe(dto) as OkObjectResult;
            result.Should().NotBeNull();
        }

        [Fact]
        public void Transcribe_Exception_Returns500()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "x" };
            _speechService.Setup(s => s.TranscribeAndSave(1, "x")).Throws(new Exception("fail"));

            var result = _controller.Transcribe(dto) as ObjectResult;
            result!.StatusCode.Should().Be(500);
        }

        // ========== GRADE SPEAKING ==========
        private void SetUserContext(int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("UserId", userId.ToString())
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public void GradeSpeaking_InvalidDto_ReturnsBadRequest()
        {
            var result = _controller.GradeSpeaking(null);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GradeSpeaking_MissingUserClaim_ReturnsUnauthorized()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO> { new SpeakingAnswerDTO { Transcript = "t" } }
            };
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = _controller.GradeSpeaking(dto);
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void GradeSpeaking_TranscriptMissing_ShouldAutoGenerateAndReturnOk()
        {
            SetUserContext(1);
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO>
                {
                    new SpeakingAnswerDTO { AudioUrl = "a", Transcript = null }
                }
            };
            _speechService.Setup(s => s.TranscribeAndSave(It.IsAny<long>(), "a")).Returns("auto");
            var jsonDoc = JsonDocument.Parse("{\"result\":\"ok\"}");
            _speakingService.Setup(s => s.GradeSpeaking(dto, 1)).Returns(jsonDoc);

            var result = _controller.GradeSpeaking(dto) as OkObjectResult;
            result.Should().NotBeNull();
        }

        [Fact]
        public void GradeSpeaking_ThrowsException_Returns500()
        {
            SetUserContext(2);
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO>
                {
                    new SpeakingAnswerDTO { Transcript = "t" }
                }
            };
            _speakingService.Setup(s => s.GradeSpeaking(dto, 2))
                .Throws(new Exception("fail"));
            var result = _controller.GradeSpeaking(dto) as ObjectResult;
            result!.StatusCode.Should().Be(500);
        }
        [Fact]
        public void GradeSpeaking_EmptyAnswers_ReturnsBadRequest()
        {
            var dto = new SpeakingGradeRequestDTO { ExamId = 1, Answers = new List<SpeakingAnswerDTO>() };

            var result = _controller.GradeSpeaking(dto);

            result.Should().BeOfType<BadRequestObjectResult>()
                  .Which.Value.Should().Be("Invalid or empty answers.");
        }

        [Fact]
        public void GradeSpeaking_WithTranscript_NoAutoTranscribe_ReturnsOk_AndDoesNotCallSpeechService()
        {
            SetUser(7);
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO>
                {
                    new SpeakingAnswerDTO { AudioUrl = "a.mp3", Transcript = "already there" }
                }
            };

            var json = JsonDocument.Parse("{\"ok\":true}");
            _speakingService.Setup(s => s.GradeSpeaking(dto, 7)).Returns(json);

            var result = _controller.GradeSpeaking(dto);

            result.Should().BeOfType<OkObjectResult>();
            _speechService.Verify(s => s.TranscribeAndSave(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GetFeedbackByExam_MultipleItems_ComputesAverageAndShapesResponse()
        {
            var items = new List<SpeakingFeedback>
    {
        new SpeakingFeedback
        {
            Overall = 8.1m,
            SpeakingAttemptId = 101,
            SpeakingAttempt = new SpeakingAttempt { SpeakingId = 11, AudioUrl = "u1", Transcript = "t1" }
        },
        new SpeakingFeedback
        {
            Overall = 7.2m,
            SpeakingAttemptId = 102,
            SpeakingAttempt = new SpeakingAttempt { SpeakingId = 12, AudioUrl = "u2", Transcript = "t2" }
        },
        new SpeakingFeedback
        {
            Overall = null,
            SpeakingAttemptId = 103,
            SpeakingAttempt = new SpeakingAttempt { SpeakingId = 13, AudioUrl = "u3", Transcript = "t3" }
        }
    };
            _feedbackService.Setup(f => f.GetByExamAndUser(5, 9)).Returns(items);

            var ok = _controller.GetFeedbackByExam(5, 9) as OkObjectResult;
            ok.Should().NotBeNull();

            var json = JsonSerializer.Serialize(ok!.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("examId").GetInt32().Should().Be(5);
            root.GetProperty("userId").GetInt32().Should().Be(9);
            root.GetProperty("totalTasks").GetInt32().Should().Be(3);

            root.GetProperty("averageOverall").GetDecimal().Should().Be(5.1m);

            var fbs = root.GetProperty("feedbacks").EnumerateArray().ToList();
            fbs.Count.Should().Be(3);
        }

        [Fact]
        public void GetFeedbackBySpeaking_ShapesResponse()
        {
            var fb = new SpeakingFeedback
            {
                SpeakingAttemptId = 22,
                Overall = 6.5m,
                SpeakingAttempt = new SpeakingAttempt
                {
                    ExamAttempt = new ExamAttempt { ExamId = 77 },
                    SpeakingId = 33,
                    AudioUrl = "aud",
                    Transcript = "txt"
                }
            };
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(33, 4)).Returns(fb);

            var ok = _controller.GetFeedbackBySpeaking(33, 4) as OkObjectResult;
            ok.Should().NotBeNull();

            var json = JsonSerializer.Serialize(ok!.Value);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("examId").GetInt32().Should().Be(77);
            root.GetProperty("userId").GetInt32().Should().Be(4);

            var inner = root.GetProperty("feedback");
            inner.GetProperty("SpeakingAttemptId").GetInt32().Should().Be(22);
            inner.GetProperty("audioUrl").GetString().Should().Be("aud");
            inner.GetProperty("transcript").GetString().Should().Be("txt");
        }


        [Fact]
        public void Create_ReturnsCreated_WithRouteValues()
        {
            var dto = new SpeakingDTO { SpeakingId = 123, SpeakingQuestion = "Q?" };
            _speakingService.Setup(s => s.Create(dto)).Returns(dto);

            var result = _controller.Create(dto).Result as CreatedAtActionResult;

            result.Should().NotBeNull();
            result!.ActionName.Should().Be(nameof(SpeakingController.GetById));
            result.RouteValues!["id"].Should().Be(123);
        }

        [Fact]
        public void Transcribe_CallsSpeechServiceOnce_WithNonEmptyTranscript()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 555, AudioUrl = "blob://x" };
            _speechService.Setup(s => s.TranscribeAndSave(555, "blob://x")).Returns("hello");

            var result = _controller.Transcribe(dto) as OkObjectResult;

            result.Should().NotBeNull();
            _speechService.Verify(s => s.TranscribeAndSave(555, "blob://x"), Times.Once);
        }
    }
}
