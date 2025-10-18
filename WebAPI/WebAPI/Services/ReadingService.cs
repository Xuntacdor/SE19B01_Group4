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

        public decimal EvaluateReading(int examId, IDictionary<int, string> answers)
        {
            var readings = _readingRepo.GetByExamId(examId);
            if (readings == null || readings.Count == 0) return 0m;

            int correct = 0;
            foreach (var r in readings)
            {
                if (answers.TryGetValue(r.ReadingId, out string? userAnswer) &&
                    string.Equals(r.CorrectAnswer, userAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    correct++;
                }
            }

            return Math.Round((decimal)correct / readings.Count * 9, 1);
        }
    }
}
