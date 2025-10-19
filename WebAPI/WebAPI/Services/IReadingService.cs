using WebAPI.Models;
using WebAPI.DTOs;
using System.Collections.Generic;

namespace WebAPI.Services
{
    public interface IReadingService
    {
        // Existing methods
        IReadOnlyList<Reading> GetReadingsByExam(int examId);
        decimal EvaluateReading(int examId, IDictionary<int, string> answers);
        
        // Missing methods that ReadingController needs
        IEnumerable<ReadingDto> GetAll();
        ReadingDto? GetById(int id);
        ReadingDto? Add(CreateReadingDto dto);
        bool Update(int id, UpdateReadingDto dto);
        bool Delete(int id);
    }
}
