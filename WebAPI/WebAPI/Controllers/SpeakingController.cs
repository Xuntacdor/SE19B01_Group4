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
        private readonly SpeechToTextService _speechService;
        private readonly ILogger<SpeakingController> _logger;

        public SpeakingController(
            ISpeakingService speakingService,
            SpeechToTextService speechService,
            ILogger<SpeakingController> logger)
        {
            _speakingService = speakingService;
            _speechService = speechService;
            _logger = logger;
        }

        // ===========================================
        // == CRUD ENDPOINTS ==
        // ===========================================

        [HttpGet("{id:int}")]
        public ActionResult<SpeakingDTO> GetById(int id)
        {
            var s = _speakingService.GetById(id);
            if (s == null) return NotFound();
            return Ok(s);
        }

        [HttpGet("exam/{examId:int}")]
        public ActionResult<IEnumerable<SpeakingDTO>> GetByExam(int examId)
        {
            var items = _speakingService.GetByExam(examId);
            return Ok(items);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public ActionResult<SpeakingDTO> Create([FromBody] SpeakingDTO dto)
        {
            if (dto == null)
                return BadRequest("Invalid request body.");

            var created = _speakingService.Create(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.SpeakingId }, created);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id:int}")]
        public ActionResult<SpeakingDTO> Update(int id, [FromBody] SpeakingDTO dto)
        {
            var updated = _speakingService.Update(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var success = _speakingService.Delete(id);
            if (!success) return NotFound();
            return NoContent();
        }

        // ===========================================
        // == TRANSCRIBE AUDIO (Whisper) ==
        // ===========================================
        /// <summary>
        /// Convert audio from Cloudinary to text and save into ExamAttempt.AnswerText.
        /// </summary>
        [Authorize]
        [HttpPost("transcribe")]
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

        // ===========================================
        // == GRADING (AI EVALUATION) ==
        // ===========================================
        /// <summary>
        /// Submit IELTS Speaking audio(s) for grading.
        /// </summary>
        /// <remarks>
        /// Accepts Cloudinary audio URLs and optional transcripts.
        /// If transcript is missing, it will be auto-generated via Whisper.
        /// </remarks>
        [Authorize]
        [HttpPost("grade")]
        public IActionResult GradeSpeaking([FromBody] SpeakingGradeRequestDTO dto)
        {
            try
            {
                if (dto == null || dto.Answers == null || !dto.Answers.Any())
                    return BadRequest("No answers provided.");

                var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized("User ID not found in token.");

                // Handle missing transcripts automatically (Speech-to-Text)
                foreach (var ans in dto.Answers)
                {
                    if (string.IsNullOrEmpty(ans.Transcript) && !string.IsNullOrEmpty(ans.AudioUrl))
                    {
                        _logger.LogInformation("[SpeakingController] Transcript missing, auto-generating for {Url}", ans.AudioUrl);
                        ans.Transcript = _speechService.TranscribeAndSave(dto.ExamId, ans.AudioUrl);
                    }
                }

                var result = _speakingService.GradeSpeaking(dto, userId);
                return Ok(JsonDocument.Parse(result.RootElement.GetRawText()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeakingController] Grading failed.");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
