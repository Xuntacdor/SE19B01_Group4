using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface ISpeakingFeedbackRepository
    {
        IEnumerable<SpeakingFeedback> GetAll();
        SpeakingFeedback? GetById(int id);
        void Add(SpeakingFeedback entity);
        void Update(SpeakingFeedback entity);
        void Delete(SpeakingFeedback entity);
        void SaveChanges();
    }
}
