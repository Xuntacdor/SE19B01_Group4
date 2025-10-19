using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

        public ListeningController(IListeningService service, IExamService examService)
        {
            _ListeningService = service;
            _examService = examService;
        }

        // Create a new Listening question (admin only)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult<ListeningDto> Create([FromBody] CreateListeningDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");
            var result = _ListeningService.Create(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ListeningId }, result);
        }

        // Get one Listening question by id
        [HttpGet("{id}")]
        public ActionResult<ListeningDto> GetById(int id)
        {
            var result = _ListeningService.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // Get all Listening questions for an exam
        [HttpGet("exam/{examId}")]
        public ActionResult<IEnumerable<ListeningDto>> GetByExam(int examId)
        {
            var list = _ListeningService.GetByExam(examId);
            return Ok(list);
        }

        // Update a Listening question (admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public ActionResult<ListeningDto> Update(int id, [FromBody] UpdateListeningDto dto)
        {
            if (dto == null) return BadRequest("Invalid payload");
            var result = _ListeningService.Update(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        // Delete a Listening question
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(int id)
        {
            var deleted = _ListeningService.Delete(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // Submit listening answers
        [HttpPost("submit")]
        public ActionResult<ExamAttemptDto> SubmitAnswers([FromBody] SubmitAttemptDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.AnswerText))
                return BadRequest("Invalid or empty payload.");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Unauthorized("Please login to submit exam.");

            try
            {
                var exam = _examService.GetById(dto.ExamId);
                if (exam == null)
                    return NotFound("Exam not found.");

                // Evaluate score only for listening module
                var score = _ListeningService.EvaluateListening(dto);

                // Attach score to DTO for persistence
                dto.Score = score;

                // Persist attempt using ExamService (shared logic)
                var attempt = _examService.SubmitAttempt(dto, userId.Value);

                // Build response DTO
                var result = new ExamAttemptDto
                {
                    AttemptId = attempt.AttemptId,
                    StartedAt = attempt.StartedAt,
                    SubmittedAt = attempt.SubmittedAt,
                    ExamId = attempt.ExamId,
                    ExamName = attempt.Exam?.ExamName ?? string.Empty,
                    ExamType = attempt.Exam?.ExamType ?? string.Empty,
                    TotalScore = attempt.Score ?? 0,
                    AnswerText = attempt.AnswerText ?? string.Empty
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Error submitting listening answers.",
                    Exception = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}
