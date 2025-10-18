using System.Collections.Generic;
using WebAPI.Models;

namespace WebAPI.Services
{
    public interface IWritingFeedbackService
    {
        /// <summary>
        /// Lấy danh sách feedback theo ExamId và UserId.
        /// </summary>
        List<WritingFeedback> GetByExamAndUser(int examId, int userId);

        /// <summary>
        /// Lấy 1 feedback cụ thể theo ID.
        /// </summary>
        WritingFeedback? GetById(int id);
    }
}
