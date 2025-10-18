using System.Collections.Generic;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public interface IWritingFeedbackRepository
    {
        WritingFeedback? GetById(int id);
        List<WritingFeedback> GetByExamAndUser(int examId, int userId);
        void Add(WritingFeedback entity);
        void Update(WritingFeedback entity);
        void Delete(WritingFeedback entity);
        void SaveChanges();
        List<WritingFeedback> GetAll();
        WritingFeedback? GetByAttemptAndWriting(long attemptId, int writingId);

    }
}
