using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models
{
    [Table("WritingFeedback")]
    public partial class WritingFeedback
    {
        [Key]
        [Column("feedback_id")]
        public int FeedbackId { get; set; }

        [Column("attempt_id")]
        public long AttemptId { get; set; }

        [Column("writing_id")]
        public int WritingId { get; set; }

        [Column("task_achievement")]
        public decimal? TaskAchievement { get; set; }

        [Column("coherence_cohesion")]
        public decimal? CoherenceCohesion { get; set; }

        [Column("lexical_resource")]
        public decimal? LexicalResource { get; set; }

        [Column("grammar_accuracy")]
        public decimal? GrammarAccuracy { get; set; }

        [Column("overall")]
        public decimal? Overall { get; set; }

        [Column("grammar_vocab_json")]
        public string? GrammarVocabJson { get; set; }

        [Column("feedback_sections")]
        public string? FeedbackSections { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [ForeignKey(nameof(AttemptId))]
        public virtual ExamAttempt? ExamAttempt { get; set; }

        [ForeignKey(nameof(WritingId))]
        public virtual Writing? Writing { get; set; }
    }
}
