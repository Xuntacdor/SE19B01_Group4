namespace WebAPI.DTOs
{
    // For creating new Listening question
    public class CreateListeningDto
    {
        public int ExamId { get; set; }

        // "Content" part – typically your audio URL or main content
        public string ListeningContent { get; set; } = string.Empty;

        // NEW: Transcript part – will be stored together with ListeningContent in the model
        public string? Transcript { get; set; }

        public string ListeningQuestion { get; set; } = string.Empty;
        public string? ListeningType { get; set; }
        public int DisplayOrder { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? QuestionHtml { get; set; }
    }

    // For updating an existing Listening question
    public class UpdateListeningDto
    {
        public string? ListeningContent { get; set; }

        // NEW: Transcript can be updated independently
        public string? Transcript { get; set; }

        public string? ListeningQuestion { get; set; }
        public string? ListeningType { get; set; }
        public int? DisplayOrder { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? QuestionHtml { get; set; }
    }

    // For returning Listening data to client
    public class ListeningDto
    {
        public int ListeningId { get; set; }
        public int ExamId { get; set; }

        // Only the "content" part (first line in DB)
        public string ListeningContent { get; set; } = string.Empty;

        // NEW: Transcript (everything after the first newline in DB)
        public string Transcript { get; set; } = string.Empty;

        public string ListeningQuestion { get; set; } = string.Empty;
        public string? ListeningType { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CorrectAnswer { get; set; }
        public string? QuestionHtml { get; set; }
    }
}
