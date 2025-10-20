using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models
{
    [Table("SpeakingAttempt")]
    public class SpeakingAttempt
    {
        [Key]
        [Column("speaking_attempt_id")]
        public long SpeakingAttemptId { get; set; }

        [Column("attempt_id")]
        public long AttemptId { get; set; }

        [Column("speaking_id")]
        public int SpeakingId { get; set; }

        [Column("audio_url")]
        public string? AudioUrl { get; set; }

        [Column("transcript")]
        public string? Transcript { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; }

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(AttemptId))]
        public ExamAttempt? ExamAttempt { get; set; }

        [ForeignKey(nameof(SpeakingId))]
        public Speaking? Speaking { get; set; }
    }
}