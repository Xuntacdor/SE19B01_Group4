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
        private readonly SpeechToTextService _speechService;
        private readonly ILogger<SpeakingController> _logger;

        public SpeakingController(
            ISpeakingService speakingService,
            ISpeakingFeedbackService feedbackService,
            SpeechToTextService speechService,
            ILogger<SpeakingController> logger)
        {
            _speakingService = speakingService;
            _feedbackService = feedbackService;
            _speechService = speechService;
            _logger = logger;
        }

        // ==========================================
        // === CRUD ENDPOINTS ===
        // ==========================================

        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult<SpeakingDTO> Create([FromBody] SpeakingDTO dto)
        {
            if (dto == null) return BadRequest("Invalid payload.");
            var result = _speakingService.Create(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.SpeakingId }, result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public ActionResult<SpeakingDTO> GetById(int id)
        {
            var result = _speakingService.GetById(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("exam/{examId}")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<SpeakingDTO>> GetByExam(int examId)
        {
            var list = _speakingService.GetByExam(examId);
            return Ok(list);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public ActionResult<SpeakingDTO> Update(int id, [FromBody] SpeakingDTO dto)
        {
            var result = _speakingService.Update(id, dto);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(int id)
        {
            var deleted = _speakingService.Delete(id);
            return deleted ? NoContent() : NotFound();
        }

        // ==========================================
        // === GET FEEDBACK ===
        // ==========================================
        [HttpGet("feedback/{examId}/{userId}")]
        [Authorize(Roles = "user,admin")]
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
                    f.SpeakingId,
                    f.Pronunciation,
                    f.Fluency,
                    f.LexicalResource,
                    f.GrammarAccuracy,
                    f.Overall,
                    f.AiAnalysisJson,
                    f.CreatedAt
                })
            };

            return Ok(response);
        }

        // ==========================================
        // === TRANSCRIBE AUDIO ===
        // ==========================================
        [HttpPost("transcribe")]
        [Authorize(Roles = "user,admin")]
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

        // ==========================================
        // === GRADE SPEAKING (AI EVALUATION) ===
        // ==========================================
        [HttpPost("grade")]
        [Authorize(Roles = "user,admin")]
        public IActionResult GradeSpeaking([FromBody] SpeakingGradeRequestDTO dto)
        {
            if (dto == null || dto.Answers == null || dto.Answers.Count == 0)
                return BadRequest("Invalid or empty answers.");

            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                                ?? User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (userIdStr == null)
                    return Unauthorized("User not logged in.");

                int userId = int.Parse(userIdStr);

                foreach (var ans in dto.Answers)
                {
                    if (string.IsNullOrEmpty(ans.Transcript) && !string.IsNullOrEmpty(ans.AudioUrl))
                    {
                        _logger.LogInformation("[SpeakingController] Transcript missing, auto-generating for {Url}", ans.AudioUrl);
                        ans.Transcript = _speechService.TranscribeAndSave(dto.ExamId, ans.AudioUrl);
                    }
                }

                var result = _speakingService.GradeSpeaking(dto, userId);
                var parsed = JsonDocument.Parse(result.RootElement.GetRawText());
                return Ok(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeakingController] Grading failed.");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
