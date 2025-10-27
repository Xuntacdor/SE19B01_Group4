using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using WebAPI.Models;

namespace WebAPI.DTOs
{
    public class ExamDto
    {
        public int ExamId { get; set; }

        public string ExamType { get; set; } = null!;

        public string ExamName { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ListeningDto> Listenings { get; set; } = new List<ListeningDto>();

        public virtual ICollection<ReadingDto> Readings { get; set; } = new List<ReadingDto>();

        public virtual ICollection<Speaking> Speakings { get; set; } = new List<Speaking>();

        public virtual ICollection<WritingDTO> Writings { get; set; } = new List<WritingDTO>();
    }
    public class UpdateExamDto
    {
        public string? ExamType { get; set; } = string.Empty;
        public string? ExamName { get; set; } = string.Empty;
    }

    public class CreateExamDto
    {
        [Required]
        public string ExamType { get; set; } = string.Empty;
        [Required]
        public string ExamName { get; set; } = string.Empty;
    }

    public class ExamAttemptSummaryDto
    {
        public long AttemptId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
    }
    public class ExamAttemptDto
    {
        public long AttemptId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string AnswerText { get; set; } = string.Empty;
    }
    public class SubmitSectionDto
    {
        public int ExamId { get; set; }
        public object Answers { get; set; } = new(); // Accepts any JSON object/array
        public DateTime StartedAt { get; set; }
    }
    public class SubmitAttemptDto
    {
        public int ExamId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public decimal Score { get; set; }
    }

    public class UserAnswerGroup
    {
        public int SkillId { get; set; }

        // Frontend sends: { "1_q1": "A", "1_q2": ["B","C"], ... }
        public Dictionary<string, object>? Answers { get; set; }

        // Normalize to string[] for grading
        public Dictionary<string, string[]> ToNormalizedMap()
        {
            var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (Answers == null || Answers.Count == 0) return map;

            foreach (var (key, val) in Answers)
            {
                try
                {
                    if (val is JsonElement je)
                    {
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.Array:
                                map[key] = je.EnumerateArray()
                                             .Select(v => v.GetString()?.Trim())
                                             .Where(s => !string.IsNullOrWhiteSpace(s))
                                             .ToArray();
                                break;
                            case JsonValueKind.String:
                                var s = je.GetString()?.Trim();
                                map[key] = string.IsNullOrEmpty(s) ? Array.Empty<string>() : new[] { s };
                                break;
                            default:
                                map[key] = new[] { je.ToString()?.Trim() ?? "_" };
                                break;
                        }
                    }
                    else if (val is IEnumerable<object> arr)
                    {
                        map[key] = arr.Select(x => x?.ToString()?.Trim())
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .ToArray();
                    }
                    else
                    {
                        var s = val?.ToString()?.Trim();
                        map[key] = string.IsNullOrEmpty(s) ? Array.Empty<string>() : new[] { s };
                    }
                }
                catch
                {
                    map[key] = Array.Empty<string>();
                }
            }

            return map;
        }
    }

}
