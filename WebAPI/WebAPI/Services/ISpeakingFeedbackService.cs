using System.Collections.Generic;
using WebAPI.Models;

namespace WebAPI.Services
{
    public interface ISpeakingFeedbackService
    {
        IEnumerable<SpeakingFeedback> GetAll();
        SpeakingFeedback? GetById(int id);
        List<SpeakingFeedback> GetByExamAndUser(int examId, int userId);
        SpeakingFeedback? GetBySpeakingAndUser(int speakingId, int userId);
        void Add(SpeakingFeedback feedback);
        void Update(SpeakingFeedback feedback);
        void Delete(SpeakingFeedback feedback);
    }
}
