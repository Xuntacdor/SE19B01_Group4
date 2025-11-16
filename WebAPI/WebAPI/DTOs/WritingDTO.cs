namespace WebAPI.DTOs
{
    public class WritingDTO
    {
        public int WritingId { get; set; }
        public int ExamId { get; set; }
        public string? WritingQuestion { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
    }

    // ========== Thêm mới phục vụ chấm bài ==========
    public class WritingGradeRequestDTO
    {
        public int ExamId { get; set; }
        public string Mode { get; set; } = "single"; 
        public List<WritingAnswerDTO> Answers { get; set; } = new();
    }

    public class WritingAnswerDTO
    {
        public int WritingId { get; set; }
        public int DisplayOrder { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
 
}
