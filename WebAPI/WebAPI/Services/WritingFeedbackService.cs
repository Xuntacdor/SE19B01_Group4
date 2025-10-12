using System.Collections.Generic;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class WritingFeedbackService : IWritingFeedbackService
    {
        private readonly IWritingFeedbackRepository _repo;

        public WritingFeedbackService(IWritingFeedbackRepository repo)
        {
            _repo = repo;
        }

        public List<WritingFeedback> GetByExamAndUser(int examId, int userId)
        {
            return _repo.GetByExamAndUser(examId, userId);
        }

        public WritingFeedback? GetById(int id)
        {
            return _repo.GetById(id);
        }
    }
}
