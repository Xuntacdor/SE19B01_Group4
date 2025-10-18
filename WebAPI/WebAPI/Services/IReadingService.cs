using WebAPI.Models;
using System.Collections.Generic;

namespace WebAPI.Services
{
    public interface IReadingService
    {
        IReadOnlyList<Reading> GetReadingsByExam(int examId);
        decimal EvaluateReading(int examId, IDictionary<int, string> answers);
    }
}
