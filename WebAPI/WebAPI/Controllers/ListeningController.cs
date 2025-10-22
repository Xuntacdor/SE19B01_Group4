using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ListeningController : ControllerBase
    {
        private readonly IListeningService _ListeningService;
        private readonly IExamService _examService;

        public ListeningController(IListeningService ListeningService, IExamService examService)
        {
            _ListeningService = ListeningService;
            _examService = examService;
        }

        // ✅ GET all Listenings
        [HttpGet]
        public ActionResult<IEnumerable<ListeningDto>> GetAll()
            => Ok(_ListeningService.GetAll());

        // ✅ GET Listening by ID (restrict to numeric ID)
        [HttpGet("{id:int}")]
        public ActionResult<ListeningDto> GetById(int id)
        {
            var Listening = _ListeningService.GetById(id);
            return Listening == null ? NotFound() : Ok(Listening);
        }

        // ✅ GET Listenings by Exam
        [HttpGet("exam/{examId:int}")]
        public ActionResult<IEnumerable<ListeningDto>> GetByExam(int examId)
        {
            var Listenings = _ListeningService.GetListeningsByExam(examId);
            var result = Listenings.Select(r => new ListeningDto
            {
                ListeningId = r.ListeningId,
                ExamId = r.ExamId,
                ListeningContent = r.ListeningContent,
                ListeningQuestion = r.ListeningQuestion,
                ListeningType = r.ListeningType,
                DisplayOrder = r.DisplayOrder,
                CorrectAnswer = r.CorrectAnswer,
                QuestionHtml = r.QuestionHtml,
                CreatedAt = r.CreatedAt
            });
            return Ok(result);
        }

        // ✅ CREATE new Listening
        [HttpPost]
        public ActionResult<ListeningDto> Add([FromBody] CreateListeningDto dto)
        {
            if (dto == null) return BadRequest("Invalid data.");

            var created = _ListeningService.Add(dto);
            if (created == null) return StatusCode(500, "Failed to create Listening.");

            return CreatedAtAction(nameof(GetById), new { id = created.ListeningId }, created);
        }

        // ✅ UPDATE existing Listening
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] UpdateListeningDto dto)
        {
            if (dto == null) return BadRequest("Invalid Listening data.");
            return _ListeningService.Update(id, dto) ? NoContent() : NotFound("Listening not found.");
        }

        // ✅ DELETE Listening
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
            => _ListeningService.Delete(id) ? NoContent() : NotFound("Listening not found.");

        // ✅ SUBMIT Listening answers by exam
        [HttpPost("submit")]
        public ActionResult<ExamAttemptDto> SubmitAnswers([FromBody] SubmitSectionDto dto)
        {
            if (dto == null || dto.Answers == null)
                return BadRequest("Invalid or empty payload.");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("Please login to submit exam.");

            try
            {
                var exam = _examService.GetById(dto.ExamId);
                if (exam == null)
                    return NotFound("Exam not found.");

                // ✅ Parse answers (controller stays responsible for decoding payload)
                var structuredAnswers = ParseAnswers(dto.Answers);
                if (structuredAnswers == null || structuredAnswers.Count == 0)
                    return BadRequest("No answers found in payload.");

                // ✅ Evaluate score
                var score = _ListeningService.EvaluateListening(dto.ExamId,structuredAnswers);

                // ✅ Build attempt data for saving
                var attemptDto = new SubmitAttemptDto
                {
                    ExamId = dto.ExamId,
                    StartedAt = dto.StartedAt,
                    AnswerText = JsonSerializer.Serialize(structuredAnswers),
                    Score = score
                };

                var attempt = _examService.SubmitAttempt(attemptDto, userId.Value);

                return Ok(new ExamAttemptDto
                {
                    AttemptId = attempt.AttemptId,
                    ExamId = attempt.ExamId,
                    ExamName = attempt.Exam?.ExamName ?? "",
                    ExamType = attempt.Exam?.ExamType ?? "",
                    StartedAt = attempt.StartedAt,
                    SubmittedAt = attempt.SubmittedAt,
                    TotalScore = attempt.Score ?? 0,
                    AnswerText = attempt.AnswerText ?? ""
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Error submitting Listening answers.",
                    Exception = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Safely parses the raw Answers object (string, JSON element, etc.)
        /// </summary>
        private List<UserAnswerGroup> ParseAnswers(object? raw)
        {
            if (raw == null) return new();

            try
            {
                string jsonString;

                if (raw is JsonElement el)
                {
                    var text = el.GetRawText();
                    jsonString = text.StartsWith("\"")
                        ? JsonSerializer.Deserialize<string>(text)!
                        : text;
                }
                else if (raw is string s)
                {
                    jsonString = s.TrimStart().StartsWith("\"")
                        ? JsonSerializer.Deserialize<string>(s)!
                        : s;
                }
                else
                {
                    jsonString = raw.ToString() ?? "[]";
                }

                return JsonSerializer.Deserialize<List<UserAnswerGroup>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
