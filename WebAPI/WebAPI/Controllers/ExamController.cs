using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamController : ControllerBase
    {
        private readonly IExamService _examService;
        private readonly IReadingService _readingService;
        private readonly IListeningService _listeningService;

        public ExamController(IExamService service,IReadingService readingService,IListeningService listeningService) 
            => (_examService,_readingService,_listeningService) = (service,readingService,listeningService);

        // ========= CREATE EXAM =========
        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult<ExamDto> Create([FromBody] CreateExamDto exam)
        {
            if (exam == null) return BadRequest("Invalid payload");

            var created = _examService.Create(exam);
            if (created == null) return BadRequest("Failed to create exam");

            // ✅ Convert entity to DTO to avoid cycles
            var dto = ConvertToDto(created);
            return CreatedAtAction(nameof(GetById), new { id = dto.ExamId }, dto);
        }

        // ========= GET EXAM BY ID =========
        [HttpGet("{id}")]
        public ActionResult<ExamDto> GetById(int id)
        {
            var exam = _examService.GetById(id);
            if (exam == null) return NotFound();

            var dto = ConvertToDto(exam);
            return Ok(dto);
        }

        [HttpGet]
        public ActionResult<IEnumerable<ExamDto>> GetAll()
        {
            var list = _examService.GetAll();

            // ✅ Map all to DTOs
            var dtoList = list.Select(ConvertToDto).ToList();

            // ✅ Use safe JSON serializer as fallback
            var jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = true
            };

            return new JsonResult(dtoList, jsonOptions);
        }

        // ========= UPDATE EXAM =========
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public ActionResult<ExamDto> Update(int id, [FromBody] UpdateExamDto exam)
        {
            if (exam == null) return BadRequest("Invalid payload");
            var updated = _examService.Update(id, exam);
            if (updated == null) return NotFound();

            return Ok(ConvertToDto(updated));
        }

        // ========= DELETE EXAM =========
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public IActionResult Delete(int id)
        {
            var deleted = _examService.Delete(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        // ========= USER ATTEMPTS =========
        [HttpGet("user/{userId}")]
        public ActionResult<IEnumerable<ExamAttemptSummaryDto>> GetExamAttemptsByUser(int userId)
        {
            var attempts = _examService.GetExamAttemptsByUser(userId);
            return Ok(attempts);
        }

        [HttpGet("attempt/{attemptId}")]
        public ActionResult<ExamAttemptSummaryDto> GetExamAttemptDetail(long attemptId)
        {
            var attempt = _examService.GetExamAttemptDetail(attemptId);
            if (attempt == null) return NotFound();

            return Ok(attempt);
        }

        // ========= PRIVATE HELPER =========
        private static ExamDto ConvertToDto(Exam exam)
        {
            return new ExamDto
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                CreatedAt = exam.CreatedAt,
                Readings = exam.Readings?.Select(r => new ReadingDto
                {
                    ReadingId = r.ReadingId,
                    ExamId = r.ExamId,
                    ReadingContent = r.ReadingContent,
                    ReadingQuestion = r.ReadingQuestion,
                    ReadingType = r.ReadingType,
                    DisplayOrder = r.DisplayOrder,
                    CreatedAt = r.CreatedAt,
                    CorrectAnswer = r.CorrectAnswer,
                    QuestionHtml = r.QuestionHtml
                }).ToList() ?? new List<ReadingDto>(),

                Listenings = exam.Listenings?.Select(r => new ListeningDto
                {
                    ListeningId = r.ListeningId,
                    ExamId = r.ExamId,
                    ListeningContent = r.ListeningContent,
                    ListeningQuestion = r.ListeningQuestion,
                    ListeningType = r.ListeningType,
                    DisplayOrder = r.DisplayOrder,
                    CreatedAt = r.CreatedAt,
                    CorrectAnswer = r.CorrectAnswer,
                    QuestionHtml = r.QuestionHtml
                }).ToList() ?? new List<ListeningDto>(),
                Speakings = exam.Speakings ?? new List<Speaking>(),
                Writings = exam.Writings?.Select(r => new WritingDTO
                {
                    WritingId = r.WritingId,
                    ExamId = r.ExamId,
                    WritingQuestion = r.WritingQuestion,
                    DisplayOrder = r.DisplayOrder,
                    CreatedAt = r.CreatedAt
                }).ToList() ?? new List<WritingDTO>()
            };
        }
    }
}
