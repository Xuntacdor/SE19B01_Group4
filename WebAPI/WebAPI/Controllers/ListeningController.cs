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
        private readonly IListeningService _listeningService;
        private readonly IExamService _examService;

        public ListeningController(IListeningService ListeningService, IExamService examService)
        {
            _listeningService = ListeningService;
            _examService = examService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListeningDto>> GetAll()
            => Ok(_listeningService.GetAll());

        [HttpGet("{id:int}")]
        public ActionResult<ListeningDto> GetById(int id)
        {
            var Listening = _listeningService.GetById(id);
            return Listening == null ? NotFound() : Ok(Listening);
        }

        [HttpGet("exam/{examId:int}")]
        public ActionResult<IEnumerable<ListeningDto>> GetByExam(int examId)
        {
            var Listenings = _listeningService.GetListeningsByExam(examId);
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

            var created = _listeningService.Add(dto);
            if (created == null) return StatusCode(500, "Failed to create Listening.");

            return CreatedAtAction(nameof(GetById), new { id = created.ListeningId }, created);
        }

        // ✅ UPDATE existing Listening
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] UpdateListeningDto dto)
        {
            if (dto == null) return BadRequest("Invalid Listening data.");
            return _listeningService.Update(id, dto) ? NoContent() : NotFound("Listening not found.");
        }

        // ✅ DELETE Listening
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
            => _listeningService.Delete(id) ? NoContent() : NotFound("Listening not found.");

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

                // Parse answers safely
                var structuredAnswers = ExamService.ParseAnswers(dto.Answers);

                // ❗ Important: treat “no actual answers” as 400
                if (structuredAnswers == null || !structuredAnswers.Any(g => g.Answers != null && g.Answers.Count > 0))
                    return BadRequest("No answers found in payload.");

                // Evaluate — any exception here is a true server error for the test
                var score = _listeningService.EvaluateListening(dto.ExamId, structuredAnswers);

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
                    ExamName = exam.ExamName,
                    ExamType = exam.ExamType,
                    StartedAt = attempt.StartedAt,
                    SubmittedAt = attempt.SubmittedAt,
                    TotalScore = attempt.Score ?? 0,
                    AnswerText = attempt.AnswerText ?? ""
                });
            }
            catch (Exception ex)
            {
                // ✅ For the test “WhenExceptionThrown_ReturnsServerError”, this must be 500
                return StatusCode(500, new
                {
                    Message = "Error submitting listening answers.",
                    Exception = ex.Message
                });
            }
        }
    }
}
