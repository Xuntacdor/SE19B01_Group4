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

                // Parse answers (handles both raw array or stringified JSON)
                List<UserAnswerGroup>? structuredAnswers = null;

                if (dto.Answers is JsonElement jsonElement)
                {
                    var raw = jsonElement.GetRawText();
                    if (raw.StartsWith("\""))
                    {
                        var inner = JsonSerializer.Deserialize<string>(raw);
                        structuredAnswers = JsonSerializer.Deserialize<List<UserAnswerGroup>>(
                            inner!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    else
                    {
                        structuredAnswers = JsonSerializer.Deserialize<List<UserAnswerGroup>>(
                            raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
                else if (dto.Answers is string jsonString)
                {
                    if (jsonString.TrimStart().StartsWith("\""))
                        jsonString = JsonSerializer.Deserialize<string>(jsonString)!;

                    structuredAnswers = JsonSerializer.Deserialize<List<UserAnswerGroup>>(
                        jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else
                {
                    structuredAnswers = JsonSerializer.Deserialize<List<UserAnswerGroup>>(
                        dto.Answers.ToString() ?? "[]",
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                if (structuredAnswers == null || structuredAnswers.Count == 0)
                    return BadRequest("No answers found in payload.");

                var answersDict = structuredAnswers
                    .Where(g => g.Answers?.Count > 0)
                    .ToDictionary(g => g.SkillId, g => g.Answers!.First());

                var score = _ListeningService.EvaluateListening(dto.ExamId, answersDict);

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
    }
}
