using System;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class SpeakingRepository : ISpeakingRepository
    {
        private readonly ApplicationDbContext _context;
        public SpeakingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Speaking? GetById(int id)
        {
            return _context.Speakings.Find(id);
        }

        public IEnumerable<Speaking> GetByExamId(int examId)
        {
            return _context.Speakings
                .Where(s => s.ExamId == examId)
                .OrderBy(s => s.DisplayOrder)
                .ToList();
        }

        public void Add(Speaking entity)
        {
            _context.Speakings.Add(entity);
        }

        public void Update(Speaking entity)
        {
            _context.Speakings.Update(entity);
        }

        public bool Delete( int id)
        {
            var existing = _context.Speakings.Find(id);
            if (existing == null) return false;

            // Xóa Feedback trước (nếu có)
            var attemptIds = _context.SpeakingAttempts
                .Where(sa => sa.SpeakingId == id)
                .Select(sa => sa.SpeakingAttemptId)
                .ToList();

            if (attemptIds.Any())
            {
                var feedbacks = _context.SpeakingFeedbacks
                    .Where(f => attemptIds.Contains(f.SpeakingAttemptId))
                    .ToList();

                if (feedbacks.Any())
                    _context.SpeakingFeedbacks.RemoveRange(feedbacks);

                var attempts = _context.SpeakingAttempts
                    .Where(sa => sa.SpeakingId == id)
                    .ToList();

                if (attempts.Any())
                    _context.SpeakingAttempts.RemoveRange(attempts);
            }

            _context.Speakings.Remove(existing);
            _context.SaveChanges();
            return true;
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
