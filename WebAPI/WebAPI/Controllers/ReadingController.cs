using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebAPI.Models;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/reading")]
    public class ReadingController : ControllerBase
    {
        private readonly IReadingService _readingService;
        private readonly IExamService _examService;

        public ReadingController(IReadingService readingService, IExamService examService)
        {
            _readingService = readingService;
            _examService = examService;
        }

        [HttpGet("exam/{examId}")]
        public ActionResult<IEnumerable<ReadingDto>> GetByExam(int examId)
        {
            var readings = _readingService.GetReadingsByExam(examId);

            // Inline mapping: entity → DTO
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

                // Parse answers: "1:A,2:B,3:C"
                var answers = dto.AnswerText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Split(':'))
                    .ToDictionary(p => int.Parse(p[0]), p => p[1]);

                // Evaluate score only for reading module
                var score = _readingService.EvaluateReading(dto.ExamId, answers);

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
                    Message = "Error submitting reading answers.",
                    Exception = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}



