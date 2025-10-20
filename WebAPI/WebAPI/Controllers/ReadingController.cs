using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReadingController : ControllerBase
    {
        private readonly IReadingService _readingService;
        private readonly IExamService _examService;

        public ReadingController(IReadingService readingService, IExamService examService)
        {
            _readingService = readingService;
            _examService = examService;
        }

        // ✅ GET all readings
        [HttpGet]
        public ActionResult<IEnumerable<ReadingDto>> GetAll()
            => Ok(_readingService.GetAll());

        // ✅ GET reading by ID (restrict to numeric ID)
        [HttpGet("{id:int}")]
        public ActionResult<ReadingDto> GetById(int id)
        {
            var reading = _readingService.GetById(id);
            return reading == null ? NotFound() : Ok(reading);
        }

        // ✅ GET readings by Exam
        [HttpGet("exam/{examId:int}")]
        public ActionResult<IEnumerable<ReadingDto>> GetByExam(int examId)
        {
            var readings = _readingService.GetReadingsByExam(examId);
            var result = readings.Select(r => new ReadingDto
            {
                ReadingId = r.ReadingId,
                ExamId = r.ExamId,
                ReadingContent = r.ReadingContent,
                ReadingQuestion = r.ReadingQuestion,
                ReadingType = r.ReadingType,
                DisplayOrder = r.DisplayOrder,
                CorrectAnswer = r.CorrectAnswer,
                QuestionHtml = r.QuestionHtml,
                CreatedAt = r.CreatedAt
            });
            return Ok(result);
        }

        // ✅ CREATE new reading
        [HttpPost]
        public ActionResult<ReadingDto> Add([FromBody] CreateReadingDto dto)
        {
            if (dto == null) return BadRequest("Invalid data.");

            var created = _readingService.Add(dto);
            if (created == null) return StatusCode(500, "Failed to create reading.");

            return CreatedAtAction(nameof(GetById), new { id = created.ReadingId }, created);
        }

        // ✅ UPDATE existing reading
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] UpdateReadingDto dto)
        {
            if (dto == null) return BadRequest("Invalid reading data.");
            return _readingService.Update(id, dto) ? NoContent() : NotFound("Reading not found.");
        }

        // ✅ DELETE reading
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
            => _readingService.Delete(id) ? NoContent() : NotFound("Reading not found.");

        // ✅ SUBMIT reading answers by exam
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

                var score = _readingService.EvaluateReading(dto.ExamId, answersDict);

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
                    Message = "Error submitting reading answers.",
                    Exception = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}
