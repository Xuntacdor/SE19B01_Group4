using System.Collections.Generic;
using System.Linq;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class SpeakingFeedbackService : ISpeakingFeedbackService
    {
        private readonly ISpeakingFeedbackRepository _repo;

        public SpeakingFeedbackService(ISpeakingFeedbackRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<SpeakingFeedback> GetAll()
        {
            return _repo.GetAll();
        }

        public SpeakingFeedback? GetById(int id)
        {
            return _repo.GetById(id);
        }

        public List<SpeakingFeedback> GetByExamAndUser(int examId, int userId)
        {
            // Sử dụng repository method với Include để load navigation properties
            return _repo.GetByExamAndUser(examId, userId);
        }

        public void Add(SpeakingFeedback feedback)
        {
            _repo.Add(feedback);
            _repo.SaveChanges();
        }

        public void Update(SpeakingFeedback feedback)
        {
            _repo.Update(feedback);
            _repo.SaveChanges();
        }

        public void Delete(SpeakingFeedback feedback)
        {
            _repo.Delete(feedback);
            _repo.SaveChanges();
        }
    }
}
