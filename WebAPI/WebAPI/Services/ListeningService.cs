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
            return listenings.Select(r => new ListeningDto
            {
                ListeningId = r.ListeningId,
                ExamId = r.ExamId,
                ListeningContent = r.ListeningContent,
                ListeningQuestion = r.ListeningQuestion,
                ListeningType = r.ListeningType,
                DisplayOrder = r.DisplayOrder,
                CorrectAnswer = r.CorrectAnswer,
                QuestionHtml = r.QuestionHtml,
                CreatedAt = r.CreatedAt
            });
        }

        public ListeningDto? GetById(int id)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return null;

            return new ListeningDto
            {
                ListeningId = listening.ListeningId,
                ExamId = listening.ExamId,
                ListeningContent = listening.ListeningContent,
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
            var listening = new Listening
            {
                ExamId = dto.ExamId,
                ListeningContent = dto.ListeningContent,
                ListeningQuestion = dto.ListeningQuestion,
                ListeningType = dto.ListeningType ?? "Markdown",
                DisplayOrder = dto.DisplayOrder,
                CorrectAnswer = dto.CorrectAnswer,
                QuestionHtml = dto.QuestionHtml,
                CreatedAt = DateTime.UtcNow
            };

            _listeningRepo.Add(listening);
            _listeningRepo.SaveChanges();

            return new ListeningDto
            {
                ListeningId = listening.ListeningId,
                ExamId = listening.ExamId,
                ListeningContent = listening.ListeningContent,
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

            if (dto.ListeningContent != null)
                listening.ListeningContent = dto.ListeningContent;
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
