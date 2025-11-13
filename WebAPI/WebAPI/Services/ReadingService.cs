using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class ReadingService : IReadingService
    {
        private readonly IReadingRepository _readingRepo;

        public ReadingService(IReadingRepository readingRepo)
        {
            _readingRepo = readingRepo;
        }

        public IReadOnlyList<Reading> GetReadingsByExam(int examId)
        {
            return _readingRepo.GetByExamId(examId);
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

        // ===================== READING =====================
        // Pass isGeneralTraining = true for GT; false for Academic
        public decimal EvaluateReading(int examId, List<UserAnswerGroup> structuredAnswers)
        {
            var readings = _readingRepo.GetByExamId(examId);
            if (readings == null || readings.Count == 0) return 0m;

            var answerMap = structuredAnswers
                .Where(g => g.Answers?.Count > 0)
                .ToDictionary(g => g.SkillId, g => g.ToNormalizedMap());

            int totalMarks = 0;
            int correctMarks = 0;

            foreach (var r in readings)
            {
                var userMap = answerMap.TryGetValue(r.ReadingId, out var um)
                    ? um
                    : new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                var correctMap = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    r.CorrectAnswer ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
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

                    totalMarks += correctVals.Length;

                    if (!userMap.TryGetValue(qKey, out var userVals) || userVals == null) continue;

                    var userSet = userVals.Select(v => v.Trim().ToLowerInvariant()).ToHashSet();
                    foreach (var opt in correctVals.Select(v => v.Trim().ToLowerInvariant()))
                        if (userSet.Contains(opt)) correctMarks++;
                }
            }

            if (totalMarks != 40) return 0m; // non full exam
            var scale = ScaleListeningAndReadingAcademic;
            return BandFrom(correctMarks, scale);
        }

        public IEnumerable<ReadingDto> GetAll()
    {
        var readings = _readingRepo.GetAll();
        return readings.Select(r => new ReadingDto
        {
            ReadingId = r.ReadingId,
            ExamId = r.ExamId,
            ReadingContent = r.ReadingContent,
            ReadingQuestion = r.ReadingQuestion,
            ReadingType = r.ReadingType,
            DisplayOrder = r.DisplayOrder,
            CorrectAnswer = r.CorrectAnswer,
            QuestionHtml = r.QuestionHtml,
            CreatedAt = r.CreatedAt
        });
    }

    public ReadingDto? GetById(int id)
    {
        var reading = _readingRepo.GetById(id);
        if (reading == null) return null;

        return new ReadingDto
        {
            ReadingId = reading.ReadingId,
            ExamId = reading.ExamId,
            ReadingContent = reading.ReadingContent,
            ReadingQuestion = reading.ReadingQuestion,
            ReadingType = reading.ReadingType,
            DisplayOrder = reading.DisplayOrder,
            CorrectAnswer = reading.CorrectAnswer,
            QuestionHtml = reading.QuestionHtml,
            CreatedAt = reading.CreatedAt
        };
    }

    public ReadingDto? Add(CreateReadingDto dto)
    {
        var reading = new Reading
        {
            ExamId = dto.ExamId,
            ReadingContent = dto.ReadingContent,
            ReadingQuestion = dto.ReadingQuestion,
            ReadingType = dto.ReadingType ?? "Markdown",
            DisplayOrder = dto.DisplayOrder,
            CorrectAnswer = dto.CorrectAnswer,
            QuestionHtml = dto.QuestionHtml,
            CreatedAt = DateTime.UtcNow
        };

        _readingRepo.Add(reading);
        _readingRepo.SaveChanges();

        return new ReadingDto
        {
            ReadingId = reading.ReadingId,
            ExamId = reading.ExamId,
            ReadingContent = reading.ReadingContent,
            ReadingQuestion = reading.ReadingQuestion,
            ReadingType = reading.ReadingType,
            DisplayOrder = reading.DisplayOrder,
            CorrectAnswer = reading.CorrectAnswer,
            QuestionHtml = reading.QuestionHtml,
            CreatedAt = reading.CreatedAt
        };
    }

    public bool Update(int id, UpdateReadingDto dto)
    {
        var reading = _readingRepo.GetById(id);
        if (reading == null) return false;

        if (dto.ReadingContent != null)
            reading.ReadingContent = dto.ReadingContent;
        if (dto.ReadingQuestion != null)
            reading.ReadingQuestion = dto.ReadingQuestion;
        if (dto.ReadingType != null)
            reading.ReadingType = dto.ReadingType;
        if (dto.DisplayOrder.HasValue)
            reading.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.CorrectAnswer != null)
            reading.CorrectAnswer = dto.CorrectAnswer;
        if (dto.QuestionHtml != null)
            reading.QuestionHtml = dto.QuestionHtml;

        _readingRepo.Update(reading);
        _readingRepo.SaveChanges();
        return true;
    }

    public bool Delete(int id)
    {
        var reading = _readingRepo.GetById(id);
        if (reading == null) return false;

        _readingRepo.Delete(reading);
        _readingRepo.SaveChanges();
        return true;
    }
}
}
