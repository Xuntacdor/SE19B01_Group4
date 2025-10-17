using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface ISpeakingRepository
    {
        Speaking? GetById(int id);
        IEnumerable<Speaking> GetByExamId(int examId);
        void Add(Speaking entity);
        void Update(Speaking entity);
        void Delete(Speaking entity);
        void SaveChanges();
    }
}
