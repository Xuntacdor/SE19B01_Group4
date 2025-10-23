using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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
                    audioUrl = f.SpeakingAttempt?.AudioUrl,       // <<< ADDED
                    transcript = f.SpeakingAttempt?.Transcript    // <<< ADDED
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
        // === TEST CLOUDINARY ACCESS ===
        // ==========================================
        [HttpPost("test-cloudinary")]
        [Authorize(Roles = "user,admin")]
        public IActionResult TestCloudinaryAccess([FromBody] SpeechTranscribeDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.AudioUrl))
                    return BadRequest("Audio URL is required.");

                var isAccessible = _speechService.TestCloudinaryAccess(dto.AudioUrl);
                return Ok(new
                {
                    audioUrl = dto.AudioUrl,
                    isAccessible,
                    message = isAccessible ? "Cloudinary URL is accessible" : "Cloudinary URL is not accessible"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeakingController] Cloudinary access test failed.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // === TRANSCRIBE AUDIO FROM FRONTEND ===
        // ==========================================
        [HttpPost("transcribe-audio")]
        [Authorize(Roles = "user,admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [ApiExplorerSettings(IgnoreApi = false)]
        public IActionResult TranscribeAudio([FromForm] AudioTranscribeDto dto)
        {
            try
            {
                if (dto.AudioFile == null || dto.AudioFile.Length == 0)
                    return BadRequest("Audio file is required.");
                if (dto.AttemptId <= 0)
                    return BadRequest("Invalid attempt ID.");

                _logger.LogInformation("[SpeakingController] Transcribing audio file: {FileName}, Size: {Size} bytes", 
                    dto.AudioFile.FileName, dto.AudioFile.Length);

                // Convert IFormFile to byte array
                using var memoryStream = new MemoryStream();
                dto.AudioFile.CopyTo(memoryStream);
                var audioBytes = memoryStream.ToArray();

                // Create temporary file
                var tempFileName = $"temp_audio_{dto.AttemptId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.webm";
                var tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), tempFileName);
                
                System.IO.File.WriteAllBytes(tempFilePath, audioBytes);

                try
                {
                    // Transcribe using temporary file
                    var transcript = _speechService.TranscribeFromFile(tempFilePath, dto.AttemptId);
                    
                    return Ok(new
                    {
                        message = "Audio transcribed successfully.",
                        attemptId = dto.AttemptId,
                        transcript
                    });
                }
                finally
                {
                    // Clean up temporary file
                    if (System.IO.File.Exists(tempFilePath))
                        System.IO.File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeakingController] Audio transcription from file failed.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ==========================================
        // === GRADE SPEAKING (AI EVALUATION) ===
        // ==========================================
        [Authorize(Policy = "VIPOnly")]
        [HttpPost("grade")]
        [Authorize(Roles = "user,admin")]
        public IActionResult GradeSpeaking([FromBody] SpeakingGradeRequestDTO dto)
        {
            if (dto == null || dto.Answers == null || dto.Answers.Count == 0)
                return BadRequest("Invalid or empty answers.");

            int userId = 0; // Initialize outside try block
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value
                                ?? User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (userIdStr == null)
                    return Unauthorized("User not logged in.");

                userId = int.Parse(userIdStr);

                foreach (var ans in dto.Answers)
                {
                    if (string.IsNullOrEmpty(ans.Transcript) && !string.IsNullOrEmpty(ans.AudioUrl))
                    {
                        _logger.LogInformation("[SpeakingController] Transcript missing, auto-generating for {Url}", ans.AudioUrl);
                        // Tạo attemptId tạm thời cho transcription
                        var tempAttemptId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        ans.Transcript = _speechService.TranscribeAndSave(tempAttemptId, ans.AudioUrl);
                        _logger.LogInformation("[SpeakingController] Generated transcript: {Transcript}", ans.Transcript);
                    }
                }

                var result = _speakingService.GradeSpeaking(dto, userId);
                var parsed = JsonDocument.Parse(result.RootElement.GetRawText());
                _logger.LogInformation("[SpeakingController] Speaking grading completed successfully for exam {ExamId}, user {UserId}", dto.ExamId, userId);
                return Ok(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SpeakingController] Speaking grading failed for exam {ExamId}, user {UserId}", dto.ExamId, userId);
                return StatusCode(500, new { error = "Grading failed", details = ex.Message });
            }
        }
    }
}
