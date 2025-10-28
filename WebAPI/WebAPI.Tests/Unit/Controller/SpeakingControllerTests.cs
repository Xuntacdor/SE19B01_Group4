using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

        // =====================================
        // === BASIC CRUD TESTS ===
        // =====================================

        [Fact]
        public void Create_WhenDtoIsNull_ReturnsBadRequest()
        {
            var result = _controller.Create(null!);
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Create_WhenSuccessful_ReturnsCreatedAt()
        {
            var dto = new SpeakingDTO { SpeakingId = 1, ExamId = 1, SpeakingQuestion = "Test" };
            _speakingService.Setup(s => s.Create(dto)).Returns(dto);

            var result = _controller.Create(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void GetById_WhenFound_ReturnsOk()
        {
            var dto = new SpeakingDTO { SpeakingId = 1, ExamId = 1 };
            _speakingService.Setup(s => s.GetById(1)).Returns(dto);

            var result = _controller.GetById(1);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_WhenNotFound_ReturnsNotFound()
        {
            _speakingService.Setup(s => s.GetById(It.IsAny<int>())).Returns((SpeakingDTO?)null);
            var result = _controller.GetById(999);
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GetByExam_ReturnsOk()
        {
            var list = new List<SpeakingDTO> { new SpeakingDTO { SpeakingId = 1 } };
            _speakingService.Setup(s => s.GetByExam(1)).Returns(list);

            var result = _controller.GetByExam(1);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNotFound()
        {
            _speakingService.Setup(s => s.Update(1, It.IsAny<SpeakingDTO>())).Returns((SpeakingDTO?)null);
            var result = _controller.Update(1, new SpeakingDTO());
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Update_WhenSuccessful_ReturnsOk()
        {
            var dto = new SpeakingDTO { SpeakingId = 1 };
            _speakingService.Setup(s => s.Update(1, dto)).Returns(dto);

            var result = _controller.Update(1, dto);
            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsNotFound()
        {
            _speakingService.Setup(s => s.Delete(1)).Returns(false);
            var result = _controller.Delete(1);
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Delete_WhenSuccessful_ReturnsNoContent()
        {
            _speakingService.Setup(s => s.Delete(1)).Returns(true);
            var result = _controller.Delete(1);
            result.Should().BeOfType<NoContentResult>();
        }

        // =====================================
        // === FEEDBACK TESTS ===
        // =====================================

        [Fact]
        public void GetFeedbackByExam_WhenEmpty_ReturnsNotFound()
        {
            _feedbackService.Setup(f => f.GetByExamAndUser(1, 1)).Returns(new List<SpeakingFeedback>());
            var result = _controller.GetFeedbackByExam(1, 1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetFeedbackByExam_WhenFound_ReturnsOk()
        {
            var feedbacks = new List<SpeakingFeedback>
            {
                new SpeakingFeedback
                {
                    SpeakingAttempt = new SpeakingAttempt { SpeakingId = 10, AudioUrl = "a.mp3", Transcript = "text" },
                    Overall = 8
                }
            };
            _feedbackService.Setup(f => f.GetByExamAndUser(1, 1)).Returns(feedbacks);

            var result = _controller.GetFeedbackByExam(1, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetFeedbackBySpeaking_WhenNotFound_ReturnsNotFound()
        {
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(1, 1)).Returns((SpeakingFeedback?)null);
            var result = _controller.GetFeedbackBySpeaking(1, 1);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void GetFeedbackBySpeaking_WhenFound_ReturnsOk()
        {
            var feedback = new SpeakingFeedback
            {
                SpeakingAttempt = new SpeakingAttempt
                {
                    SpeakingId = 5,
                    Transcript = "abc",
                    AudioUrl = "audio.mp3",
                    ExamAttempt = new ExamAttempt { ExamId = 2 }
                },
                Overall = 9
            };
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(5, 1)).Returns(feedback);
            var result = _controller.GetFeedbackBySpeaking(5, 1);
            result.Should().BeOfType<OkObjectResult>();
        }

        // =====================================
        // === TRANSCRIBE TESTS ===
        // =====================================

        [Fact]
        public void Transcribe_WhenDtoIsNull_ReturnsBadRequest()
        {
            var result = _controller.Transcribe(null!);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_WhenAudioUrlMissing_ReturnsBadRequest()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "" };
            var result = _controller.Transcribe(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_WhenAttemptInvalid_ReturnsBadRequest()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 0, AudioUrl = "http://audio.mp3" };
            var result = _controller.Transcribe(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Transcribe_WhenSuccess_ReturnsOk()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "http://audio.mp3" };

            _speechService
                .Setup(s => s.TranscribeAndSave(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("mock transcript");

            var result = _controller.Transcribe(dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Transcribe_WhenException_Returns500()
        {
            var dto = new SpeechTranscribeDto { AttemptId = 1, AudioUrl = "x" };
            _speechService
     .Setup(s => s.TranscribeAndSave(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
     .Throws(new Exception("fail"));

            var result = _controller.Transcribe(dto);
            result.Should().BeOfType<ObjectResult>();
            ((ObjectResult)result).StatusCode.Should().Be(500);
        }

        // =====================================
        // === CLOUDINARY TESTS ===
        // =====================================

        [Fact]
        public void TestCloudinaryAccess_WhenAudioUrlMissing_ReturnsBadRequest()
        {
            var dto = new SpeechTranscribeDto { AudioUrl = "" };
            var result = _controller.TestCloudinaryAccess(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void TestCloudinaryAccess_WhenAccessible_ReturnsOk()
        {
            var dto = new SpeechTranscribeDto { AudioUrl = "http://valid" };
            _speechService.Setup(s => s.TestCloudinaryAccess(dto.AudioUrl)).Returns(true);
            var result = _controller.TestCloudinaryAccess(dto);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void TestCloudinaryAccess_WhenException_Returns500()
        {
            var dto = new SpeechTranscribeDto { AudioUrl = "x" };
            _speechService.Setup(s => s.TestCloudinaryAccess(It.IsAny<string>()))
                          .Throws(new Exception("fail"));
            var result = _controller.TestCloudinaryAccess(dto);
            result.Should().BeOfType<ObjectResult>();
            ((ObjectResult)result).StatusCode.Should().Be(500);
        }

        // =====================================
        // === GRADE SPEAKING ===
        // =====================================

        [Fact]
        public void GradeSpeaking_WhenInvalidInput_ReturnsBadRequest()
        {
            var dto = new SpeakingGradeRequestDTO { Answers = null! };
            var result = _controller.GradeSpeaking(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GradeSpeaking_WhenException_ReturnsServerError()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO>
                {
                    new SpeakingAnswerDTO { AudioUrl = "a.mp3", Transcript = "hello" }
                }
            };
            _speakingService.Setup(s => s.GradeSpeaking(It.IsAny<SpeakingGradeRequestDTO>(), It.IsAny<int>()))
                            .Throws(new Exception("fail"));

            var result = _controller.GradeSpeaking(dto);
            result.Should().BeOfType<ObjectResult>();
            ((ObjectResult)result).StatusCode.Should().Be(500);
        }
        [Fact]
        public void TranscribeAudio_WhenFileIsMissing_ReturnsBadRequest()
        {
            var dto = new AudioTranscribeDto { AttemptId = 1, AudioFile = null! };
            var result = _controller.TranscribeAudio(dto);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void TranscribeAudio_WhenExceptionThrown_Returns500()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(10);
            mockFile.Setup(f => f.CopyTo(It.IsAny<Stream>())).Throws(new IOException("fail"));

            var dto = new AudioTranscribeDto { AttemptId = 1, AudioFile = mockFile.Object };
            var result = _controller.TranscribeAudio(dto);

            result.Should().BeOfType<ObjectResult>();
            ((ObjectResult)result).StatusCode.Should().Be(500);
        }
        [Fact]
        public void GradeSpeaking_WhenUserNotLoggedIn_ReturnsUnauthorized()
        {
            // Arrange
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO>
        {
            new SpeakingAnswerDTO { AudioUrl = "http://audio.mp3", Transcript = "test" }
        }
            };

            // simulate empty HttpContext (no claims)
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = _controller.GradeSpeaking(dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            ((UnauthorizedObjectResult)result).Value.Should().Be("User not logged in.");
        }

        [Fact]
        public void TestCloudinaryAccess_WhenNotAccessible_ReturnsOkWithFalse()
        {
            var dto = new SpeechTranscribeDto { AudioUrl = "http://fake" };
            _speechService.Setup(s => s.TestCloudinaryAccess(It.IsAny<string>())).Returns(false);

            var result = _controller.TestCloudinaryAccess(dto);
            result.Should().BeOfType<OkObjectResult>();
            var obj = result as OkObjectResult;
            obj!.Value.ToString().Should().Contain("not accessible");
        }

    }

}
