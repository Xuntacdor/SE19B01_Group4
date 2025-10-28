using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface ISpeakingRepository
    {
        Speaking? GetById(int id);
        IEnumerable<Speaking> GetByExamId(int examId);
        void Add(Speaking entity);
        void Update(Speaking entity);
        bool Delete(int id);
        void SaveChanges();
        SpeakingAttempt GetOrCreateAttempt(int examId, int speakingId, int userId, string? audioUrl, string? transcript);
    }
}
