using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class ListeningService : IListeningService
    {
        private readonly IListeningRepository _listeningRepo;

        public ListeningService(IListeningRepository listeningRepo)
        {
            _listeningRepo = listeningRepo;
        }

        public IReadOnlyList<Listening> GetListeningsByExam(int examId)
        {
            return _listeningRepo.GetByExamId(examId);
        }

        // ===================== helper: content + transcript =====================
        /// <summary>
        /// Split stored content into "content" (first line) and "transcript" (rest).
        /// Old data (no newline) => transcript = "" and content = original value.
        /// </summary>
        private static (string Content, string Transcript) SplitContentAndTranscript(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return (string.Empty, string.Empty);

            var normalized = raw.Replace("\r\n", "\n");
            var parts = normalized.Split('\n', 2, StringSplitOptions.None);

            var content = parts[0];
            var transcript = parts.Length > 1 ? parts[1] : string.Empty;
            return (content, transcript);
        }

        /// <summary>
        /// Merge "content" and "transcript" into one string for the model.
        /// If transcript is null/empty, returns just content (old behavior).
        /// </summary>
        private static string MergeContentAndTranscript(string? content, string? transcript)
        {
            content ??= string.Empty;
            if (string.IsNullOrWhiteSpace(transcript))
                return content;

            return content + "\n" + transcript;
        }

        // ===================== IELTS BAND TABLES =====================
        private static readonly (int Min, int Max, decimal Band)[] ScaleListeningAndReadingAcademic = new[]
        {
            (39, 40, 9.0m),
            (37, 38, 8.5m),
            (35, 36, 8.0m),
            (33, 34, 7.5m),
            (30, 32, 7.0m),
            (27, 29, 6.5m),
            (23, 26, 6.0m),
            (20, 22, 5.5m),
            (16, 19, 5.0m),
            (13, 15, 4.5m),
            (10, 12, 4.0m),
            ( 7,  9, 3.5m),
            ( 5,  6, 3.0m),
            ( 3,  4, 2.5m),
            ( 0,  2, 2.0m),
        };

        private static decimal BandFrom(int correct, (int Min, int Max, decimal Band)[] scale)
        {
            foreach (var row in scale)
                if (correct >= row.Min && correct <= row.Max)
                    return row.Band;
            return 0m;
        }

        // ===================== LISTENING =====================
        public decimal EvaluateListening(int examId, List<UserAnswerGroup> structuredAnswers)
        {
            var listenings = _listeningRepo.GetByExamId(examId);
            if (listenings == null || listenings.Count == 0) return 0m;

            var answerMap = structuredAnswers
                .Where(g => g.Answers?.Count > 0)
                .ToDictionary(g => g.SkillId, g => g.ToNormalizedMap());

            int totalMarks = 0;
            int correctMarks = 0;

            foreach (var l in listenings)
            {
                // use empty map if user has no answers for this part
                var userMap = answerMap.TryGetValue(l.ListeningId, out var um)
                    ? um
                    : new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                var correctMap = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    l.CorrectAnswer ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new();

                foreach (var (qKey, je) in correctMap)
                {
                    var correctVals = je.ValueKind switch
                    {
                        JsonValueKind.Array => je.EnumerateArray().Select(x => x.GetString()?.Trim() ?? "")
                            .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
                        JsonValueKind.String => new[] { je.GetString()?.Trim() ?? "" },
                        _ => new[] { je.ToString()?.Trim() ?? "" }
                    };

                    // count total from the key (ground truth), not from user answers
                    totalMarks += correctVals.Length;

                    if (!userMap.TryGetValue(qKey, out var userVals) || userVals == null) continue;

                    var userSet = userVals.Select(v => v.Trim().ToLowerInvariant()).ToHashSet();
                    foreach (var opt in correctVals.Select(v => v.Trim().ToLowerInvariant()))
                        if (userSet.Contains(opt)) correctMarks++;
                }
            }

            if (totalMarks != 40) return 0m; // non full exam
            return BandFrom(correctMarks, ScaleListeningAndReadingAcademic);
        }

        public IEnumerable<ListeningDto> GetAll()
        {
            var listenings = _listeningRepo.GetAll();
            return listenings.Select(r =>
            {
                var (content, transcript) = SplitContentAndTranscript(r.ListeningContent);
                return new ListeningDto
                {
                    ListeningId = r.ListeningId,
                    ExamId = r.ExamId,
                    ListeningContent = content,
                    Transcript = transcript,
                    ListeningQuestion = r.ListeningQuestion,
                    ListeningType = r.ListeningType,
                    DisplayOrder = r.DisplayOrder,
                    CorrectAnswer = r.CorrectAnswer,
                    QuestionHtml = r.QuestionHtml,
                    CreatedAt = r.CreatedAt
                };
            });
        }

        public ListeningDto? GetById(int id)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return null;

            var (content, transcript) = SplitContentAndTranscript(listening.ListeningContent);

            return new ListeningDto
            {
                ListeningId = listening.ListeningId,
                ExamId = listening.ExamId,
                ListeningContent = content,
                Transcript = transcript,
                ListeningQuestion = listening.ListeningQuestion,
                ListeningType = listening.ListeningType,
                DisplayOrder = listening.DisplayOrder,
                CorrectAnswer = listening.CorrectAnswer,
                QuestionHtml = listening.QuestionHtml,
                CreatedAt = listening.CreatedAt
            };
        }

        public ListeningDto? Add(CreateListeningDto dto)
        {
            var mergedContent = MergeContentAndTranscript(dto.ListeningContent, dto.Transcript);

            var listening = new Listening
            {
                ExamId = dto.ExamId,
                ListeningContent = mergedContent,
                ListeningQuestion = dto.ListeningQuestion,
                ListeningType = dto.ListeningType ?? "Markdown",
                DisplayOrder = dto.DisplayOrder,
                CorrectAnswer = dto.CorrectAnswer,
                QuestionHtml = dto.QuestionHtml,
                CreatedAt = DateTime.UtcNow
            };

            _listeningRepo.Add(listening);
            _listeningRepo.SaveChanges();

            var (content, transcript) = SplitContentAndTranscript(listening.ListeningContent);

            return new ListeningDto
            {
                ListeningId = listening.ListeningId,
                ExamId = listening.ExamId,
                ListeningContent = content,
                Transcript = transcript,
                ListeningQuestion = listening.ListeningQuestion,
                ListeningType = listening.ListeningType,
                DisplayOrder = listening.DisplayOrder,
                CorrectAnswer = listening.CorrectAnswer,
                QuestionHtml = listening.QuestionHtml,
                CreatedAt = listening.CreatedAt
            };
        }

        public bool Update(int id, UpdateListeningDto dto)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return false;

            // Handle content + transcript together for backward compatibility
            if (dto.ListeningContent != null || dto.Transcript != null)
            {
                var existing = SplitContentAndTranscript(listening.ListeningContent);

                var newContent = dto.ListeningContent ?? existing.Content;
                var newTranscript = dto.Transcript ?? existing.Transcript;

                listening.ListeningContent = MergeContentAndTranscript(newContent, newTranscript);
            }

            if (dto.ListeningQuestion != null)
                listening.ListeningQuestion = dto.ListeningQuestion;
            if (dto.ListeningType != null)
                listening.ListeningType = dto.ListeningType;
            if (dto.DisplayOrder.HasValue)
                listening.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.CorrectAnswer != null)
                listening.CorrectAnswer = dto.CorrectAnswer;
            if (dto.QuestionHtml != null)
                listening.QuestionHtml = dto.QuestionHtml;

            _listeningRepo.Update(listening);
            _listeningRepo.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return false;

            _listeningRepo.Delete(listening);
            _listeningRepo.SaveChanges();
            return true;
        }
    }
}
