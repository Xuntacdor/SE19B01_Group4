using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using WebAPI.Models;
using WebAPI.DTOs;
using WebAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;

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

        // ✅ GET all readings
        [HttpGet]
        public ActionResult<IEnumerable<ReadingDto>> GetAll()
        {
            var readings = _readingService.GetAll();
            return Ok(readings);
        }

        // ✅ GET reading by ID
        [HttpGet("{id}")]
        public ActionResult<ReadingDto> GetById(int id)
        {
            var reading = _readingService.GetById(id);
            if (reading == null)
                return NotFound();
            return Ok(reading);
        }

        // ✅ GET readings by Exam
        [HttpGet("exam/{examId}")]
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
            if (dto == null)
                return BadRequest("Invalid data.");

            var created = _readingService.Add(dto);
            if (created == null)
                return StatusCode(500, "Failed to create reading.");

            return CreatedAtAction(nameof(GetById), new { id = created.ReadingId }, created);
        }

        // ✅ UPDATE existing reading
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateReadingDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid reading data.");

            var success = _readingService.Update(id, dto);
            if (!success)
                return NotFound("Reading not found.");

            return NoContent();
        }

        // ✅ DELETE reading
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var success = _readingService.Delete(id);
            if (!success)
                return NotFound("Reading not found.");

            return NoContent();
        }

        // ✅ SUBMIT reading attempt (for user exams)
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

                // Parse "1:A,2:B,3:C"
                var answers = dto.AnswerText
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Split(':'))
                    .ToDictionary(p => int.Parse(p[0]), p => p[1]);

                var score = _readingService.EvaluateReading(dto.ExamId, answers);
                dto.Score = score;

                var attempt = _examService.SubmitAttempt(dto, userId.Value);

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
