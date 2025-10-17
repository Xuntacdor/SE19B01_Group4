namespace WebAPI.Models
{
    public class SpeakingFeedback
    {
        public int FeedbackId { get; set; }
        public long AttemptId { get; set; } // FK → ExamAttempt
        public int SpeakingId { get; set; } // FK → Speaking
        public decimal? Pronunciation { get; set; }
        public decimal? Fluency { get; set; }
        public decimal? LexicalResource { get; set; }
        public decimal? GrammarAccuracy { get; set; }
        public decimal? Coherence { get; set; }
        public decimal? Overall { get; set; }
        public string? AiAnalysisJson { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ExamAttempt Attempt { get; set; } = null!;
        public Speaking Speaking { get; set; } = null!;
    }
}
