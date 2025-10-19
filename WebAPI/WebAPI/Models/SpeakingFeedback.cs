using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models
{
    [Table("SpeakingFeedback")]
    public class SpeakingFeedback
    {
        [Key]
        [Column("feedback_id")]
        public int FeedbackId { get; set; }

        [Column("speaking_attempt_id")]
        public long SpeakingAttemptId { get; set; }

        [Column("pronunciation")]
        public decimal? Pronunciation { get; set; }

        [Column("fluency")]
        public decimal? Fluency { get; set; }

        [Column("lexical_resource")]
        public decimal? LexicalResource { get; set; }

        [Column("grammar_accuracy")]
        public decimal? GrammarAccuracy { get; set; }

        [Column("coherence")]
        public decimal? Coherence { get; set; }

        [Column("overall")]
        public decimal? Overall { get; set; }

        [Column("ai_analysis_json")]
        public string? AiAnalysisJson { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(SpeakingAttemptId))]
        public SpeakingAttempt? SpeakingAttempt { get; set; }
    }
}
