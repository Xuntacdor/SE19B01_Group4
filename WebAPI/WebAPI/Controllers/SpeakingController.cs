using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpeakingController : ControllerBase
    {
        private readonly ISpeakingService _speakingService;
        private readonly ISpeakingFeedbackService _feedbackService;
        private readonly ISpeechToTextService _speechService;
        private readonly ILogger<SpeakingController> _logger;

        public SpeakingController(
            ISpeakingService speakingService,
            ISpeakingFeedbackService feedbackService,
            ISpeechToTextService speechService,
            ILogger<SpeakingController> logger)
        {
            _speakingService = speakingService;
            _feedbackService = feedbackService;
            _speechService = speechService;
            _logger = logger;
        }

        // ✅ FIXED: Use IActionResult (not ActionResult<T>)
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Create([FromBody] SpeakingDTO dto)
        {
            if (dto == null)
                return BadRequest("Invalid payload.");

            var result = _speakingService.Create(dto);
            if (result == null)
                return BadRequest("Failed to create Speaking record.");

            return CreatedAtAction(nameof(GetById), new { id = result.SpeakingId }, result);
        }

        // ✅ FIXED: Return IActionResult
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetById(int id)
        {
            var result = _speakingService.GetById(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        // ✅ FIXED: Return IActionResult
        [HttpGet("exam/{examId}")]
        [AllowAnonymous]
        public IActionResult GetByExam(int examId)
        {
            var list = _speakingService.GetByExam(examId);
            return Ok(list);
        }

        [HttpPut("{id}")]
        [AllowAnonymous]
        public IActionResult Update(int id, [FromBody] SpeakingDTO dto)
        {
            var result = _speakingService.Update(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id}")]
        [AllowAnonymous]
        public IActionResult Delete(int id)
        {
            var deleted = _speakingService.Delete(id);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("feedback/{examId}/{userId}")]
        [AllowAnonymous]
        public IActionResult GetFeedbackByExam(int examId, int userId)
        {
            var feedbacks = _feedbackService.GetByExamAndUser(examId, userId);
            if (feedbacks == null || feedbacks.Count == 0)
                return NotFound(new { message = "No feedback found for this exam." });

            var response = new
            {
                examId,
                userId,
                totalTasks = feedbacks.Count,
                averageOverall = Math.Round(feedbacks.Average(f => f.Overall ?? 0), 1),
                feedbacks = feedbacks.Select(f => new
                {
                    speakingId = f.SpeakingAttempt?.SpeakingId,
                    f.SpeakingAttemptId,
                    f.Pronunciation,
                    f.Fluency,
                    f.LexicalResource,
                    f.GrammarAccuracy,
                    f.Coherence,
                    f.Overall,
                    f.AiAnalysisJson,
                    f.CreatedAt,
                    audioUrl = f.SpeakingAttempt?.AudioUrl,
                    transcript = f.SpeakingAttempt?.Transcript
                })
            };
            return Ok(response);
        }

        [HttpGet("feedback/bySpeaking")]
        [AllowAnonymous]
        public IActionResult GetFeedbackBySpeaking(int speakingId, int userId)
        {
            var feedback = _feedbackService.GetBySpeakingAndUser(speakingId, userId);
            if (feedback == null)
                return NotFound(new { message = "No feedback found for this speaking task." });

            var response = new
            {
                examId = feedback.SpeakingAttempt?.ExamAttempt?.ExamId,
                userId,
                feedback = new
                {
                    speakingId = feedback.SpeakingAttempt?.SpeakingId,
                    feedback.SpeakingAttemptId,
                    feedback.Pronunciation,
                    feedback.Fluency,
                    feedback.LexicalResource,
                    feedback.GrammarAccuracy,
                    feedback.Coherence,
                    feedback.Overall,
                    feedback.AiAnalysisJson,
                    feedback.CreatedAt,
                    audioUrl = feedback.SpeakingAttempt?.AudioUrl,
                    transcript = feedback.SpeakingAttempt?.Transcript
                }
            };
            return Ok(response);
        }

        [HttpPost("transcribe")]
        [AllowAnonymous]
        public IActionResult Transcribe([FromBody] SpeechTranscribeDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.AudioUrl))
                    return BadRequest("Audio URL is required.");
                if (dto.AttemptId <= 0)
                    return BadRequest("Invalid attempt ID.");

                var transcript = _speechService.TranscribeAndSave(dto.AttemptId, dto.AudioUrl);
                return Ok(new
                {
                    message = "Audio transcribed and saved successfully.",
                    attemptId = dto.AttemptId,
                    transcript
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeakingController] Transcription failed.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("grade")]
        [AllowAnonymous]
        public IActionResult GradeSpeaking([FromBody] SpeakingGradeRequestDTO dto)
        {
            if (dto == null || dto.Answers == null || dto.Answers.Count == 0)
                return BadRequest("Invalid or empty answers.");

            int userId = 0;

            try
            {
                // ✅ Safely retrieve userId from claims (avoid NullReferenceException)
                var userIdStr = HttpContext?.User?.Claims?
                    .FirstOrDefault(c => c.Type == "UserId")?.Value
                    ?? HttpContext?.User?.Claims?
                    .FirstOrDefault(c => c.Type == "id")?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                {
                    return new ObjectResult(new { message = "User not logged in." })
                    {
                        StatusCode = StatusCodes.Status401Unauthorized
                    };
                }

                userId = int.Parse(userIdStr);

                // ✅ Convert audio to transcript if missing
                foreach (var ans in dto.Answers)
                {
                    if (string.IsNullOrEmpty(ans.Transcript) && !string.IsNullOrEmpty(ans.AudioUrl))
                    {
                        var tempAttemptId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        ans.Transcript = _speechService.TranscribeAndSave(tempAttemptId, ans.AudioUrl);
                    }
                }

                // ✅ Grade with AI service
                var result = _speakingService.GradeSpeaking(dto, userId);

                // ✅ Ensure valid JSON result before returning
                var parsed = JsonDocument.Parse(result.RootElement.GetRawText());
                return Ok(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SpeakingController] Speaking grading failed for exam {ExamId}, user {UserId}",
                    dto?.ExamId, userId);

                return StatusCode(500, new
                {
                    error = "Grading failed",
                    details = ex.Message
                });
            }
        }

    }
}
