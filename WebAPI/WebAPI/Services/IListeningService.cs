using WebAPI.Models;
using WebAPI.DTOs;
using System.Collections.Generic;

namespace WebAPI.Services
{
    public interface IListeningService
    {
        IReadOnlyList<Listening> GetListeningsByExam(int examId);
        decimal EvaluateListening(int examId, List<UserAnswerGroup> structuredAnswers);
        IEnumerable<ListeningDto> GetAll();
        ListeningDto? GetById(int id);
        ListeningDto? Add(CreateListeningDto dto);
        bool Update(int id, UpdateListeningDto dto);
        bool Delete(int id);
    }
}
