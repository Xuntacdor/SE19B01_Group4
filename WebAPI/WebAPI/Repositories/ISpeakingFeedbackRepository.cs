using System.Collections.Generic;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface ISpeakingFeedbackRepository
    {
        IEnumerable<SpeakingFeedback> GetAll();
        SpeakingFeedback? GetById(int id);
        List<SpeakingFeedback> GetByExamAndUser(int examId, int userId);
        void Add(SpeakingFeedback entity);
        void Update(SpeakingFeedback entity);
        void Delete(SpeakingFeedback entity);
        void SaveChanges();
    }
}
