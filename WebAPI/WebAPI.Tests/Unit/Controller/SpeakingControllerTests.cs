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
using System.Linq;

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

        [Fact]
        public void GradeSpeaking_ShouldReturn500_WhenUserIdClaimNotInt()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO> { new() { AudioUrl = "a" } }
            };
            
            var json = JsonDocument.Parse("{\"ok\":true}");
            _speakingService.Setup(s => s.GradeSpeaking(dto, It.IsAny<int>())).Returns(json);
            
            var context = new DefaultHttpContext();
            var claims = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("UserId", "notAnInt")
            }, "mock");
            context.User = new System.Security.Claims.ClaimsPrincipal(claims);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };

            var result = _controller.GradeSpeaking(dto);
            var obj = result.Should().BeOfType<ObjectResult>().Subject;
            obj.StatusCode.Should().Be(500);
        }

        [Fact]
        public void GradeSpeaking_ShouldTranscribeAudio_WhenTranscriptMissing()
        {
            var dto = new SpeakingGradeRequestDTO
            {
                ExamId = 1,
                Answers = new List<SpeakingAnswerDTO> { new() { AudioUrl = "a", Transcript = null } }
            };
            var json = JsonDocument.Parse("{\"ok\":true}");
            _speakingService.Setup(s => s.GradeSpeaking(dto, It.IsAny<int>())).Returns(json);
            _speechService.Setup(s => s.TranscribeAndSave(It.IsAny<long>(), "a")).Returns("auto-transcribed");
            var context = new DefaultHttpContext();
            var claims = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("UserId", "3")
            }, "mock");
            context.User = new System.Security.Claims.ClaimsPrincipal(claims);
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
            var result = _controller.GradeSpeaking(dto);
            result.Should().BeOfType<OkObjectResult>();
            dto.Answers[0].Transcript.Should().Be("auto-transcribed");
        }

        [Fact]
        public void GetFeedbackByExam_ShouldHandleAllOverallNull()
        {
            var list = new List<SpeakingFeedback>
            {
                new() { Overall = null, SpeakingAttempt = new SpeakingAttempt { SpeakingId = 1 } },
                new() { Overall = null, SpeakingAttempt = new SpeakingAttempt { SpeakingId = 2 } },
            };
            _feedbackService.Setup(f => f.GetByExamAndUser(9, 7)).Returns(list);
            var result = _controller.GetFeedbackByExam(9, 7);
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetFeedbackByExam_Should_Map_All_Feedback_Properties()
        {
            var now = DateTime.UtcNow;
            var feedbacks = new List<SpeakingFeedback>
            {
                new SpeakingFeedback
                {
                    SpeakingAttempt = new SpeakingAttempt
                    {
                        SpeakingId = 123,
                        AudioUrl = "url",
                        Transcript = "transcript"
                    },
                    Pronunciation = 1, Fluency = 2, LexicalResource = 3, GrammarAccuracy = 4, Coherence = 5,
                    Overall = 6, AiAnalysisJson = "json-ai", CreatedAt = now, SpeakingAttemptId = 111
                },
                new SpeakingFeedback { SpeakingAttempt = null, SpeakingAttemptId = 112 }
            };
            _feedbackService.Setup(f => f.GetByExamAndUser(42, 2)).Returns(feedbacks);
            var result = _controller.GetFeedbackByExam(42, 2) as OkObjectResult;
            result.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var props = root.EnumerateObject().ToList();
            var totalTasksProp = props.FirstOrDefault(p => p.Name.ToLower() == "totaltasks");
            var fbArrProp = props.FirstOrDefault(p => p.Name.ToLower() == "feedbacks");
            Assert.False(totalTasksProp.Equals(default), $"totalTasks property not found. Available: {string.Join(",", props.Select(p => p.Name))}");
            Assert.False(fbArrProp.Equals(default), $"feedbacks property not found. Available: {string.Join(",", props.Select(p => p.Name))}");
            Assert.Equal(2, totalTasksProp.Value.GetInt32());
            var fbArr = fbArrProp.Value;
            Assert.Equal(System.Text.Json.JsonValueKind.Array, fbArr.ValueKind);
            Assert.Equal(2, fbArr.GetArrayLength());
            var mainFb = fbArr[0];
            string[] childProps = {"speakingid","audioUrl","transcript","pronunciation","lexicalResource","grammarAccuracy","aiAnalysisJson","speakingAttemptId" };
            foreach (var child in childProps)
                Assert.True(mainFb.EnumerateObject().Any(x => x.Name.ToLower() == child.ToLower()), $"Child property {child} missing! Actual: {string.Join(",", mainFb.EnumerateObject().Select(x => x.Name))}");
            Assert.Equal(123, mainFb.EnumerateObject().First(p => p.Name.ToLower() == "speakingid").Value.GetInt32());
            Assert.Equal("url", mainFb.EnumerateObject().First(p => p.Name.ToLower() == "audiourl").Value.GetString());
            Assert.Equal("transcript", mainFb.EnumerateObject().First(p => p.Name.ToLower() == "transcript").Value.GetString());
            Assert.Equal(1, mainFb.EnumerateObject().First(p => p.Name.ToLower() == "pronunciation").Value.GetInt32());
            Assert.Equal(3, mainFb.EnumerateObject().First(p => p.Name.ToLower() == "lexicalresource").Value.GetInt32());
            Assert.Equal(4, mainFb.EnumerateObject().First(p => p.Name.ToLower() == "grammaraccuracy").Value.GetInt32());
            Assert.Equal("json-ai", mainFb.EnumerateObject().First(p => p.Name.ToLower() == "aianalysisjson").Value.GetString());
            Assert.Equal(111, mainFb.EnumerateObject().First(p => p.Name.ToLower() == "speakingattemptid").Value.GetInt32());
            var nullFb = fbArr[1];
            Assert.True(nullFb.EnumerateObject().First(p => p.Name.ToLower() == "speakingid").Value.ValueKind == System.Text.Json.JsonValueKind.Null);
            Assert.True(nullFb.EnumerateObject().First(p => p.Name.ToLower() == "audiourl").Value.ValueKind == System.Text.Json.JsonValueKind.Null);
            Assert.True(nullFb.EnumerateObject().First(p => p.Name.ToLower() == "transcript").Value.ValueKind == System.Text.Json.JsonValueKind.Null);
            Assert.Equal(112, nullFb.EnumerateObject().First(p => p.Name.ToLower() == "speakingattemptid").Value.GetInt32());
        }

        [Fact]
        public void GetFeedbackBySpeaking_Should_Cover_All_Response_Properties_With_Nulls()
        {
            var fb = new SpeakingFeedback { SpeakingAttempt = null, SpeakingAttemptId = 33 };
            _feedbackService.Setup(f => f.GetBySpeakingAndUser(8, 4)).Returns(fb);
            var result = _controller.GetFeedbackBySpeaking(8, 4) as OkObjectResult;
            result.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var props = root.EnumerateObject().ToList();
            var fbProp = props.FirstOrDefault(p => p.Name.ToLower() == "feedback");
            Assert.False(fbProp.Equals(default), $"feedback property not found. Actual: {string.Join("|", props.Select(p => p.Name))}");
            var fbDetail = fbProp.Value;
            Assert.True(fbDetail.EnumerateObject().First(p => p.Name.ToLower() == "speakingid").Value.ValueKind == System.Text.Json.JsonValueKind.Null);
            Assert.True(fbDetail.EnumerateObject().First(p => p.Name.ToLower() == "audiourl").Value.ValueKind == System.Text.Json.JsonValueKind.Null);
            Assert.True(fbDetail.EnumerateObject().First(p => p.Name.ToLower() == "transcript").Value.ValueKind == System.Text.Json.JsonValueKind.Null);
            Assert.Equal(33, fbDetail.EnumerateObject().First(p => p.Name.ToLower() == "speakingattemptid").Value.GetInt32());
            // examId property null
            var examIdProp = props.FirstOrDefault(p => p.Name.ToLower() == "examid");
            Assert.False(examIdProp.Equals(default), $"examId not found in {string.Join("|", props.Select(p => p.Name))}");
            Assert.True(examIdProp.Value.ValueKind == System.Text.Json.JsonValueKind.Null);
        }

    }
}
