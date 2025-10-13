using System.Collections.Generic;
using WebAPI.Models;

namespace WebAPI.Services
{
    public interface IWritingFeedbackService
    {
        List<WritingFeedback> GetByExamAndUser(int examId, int userId);
        WritingFeedback? GetById(int id);
    }
}
