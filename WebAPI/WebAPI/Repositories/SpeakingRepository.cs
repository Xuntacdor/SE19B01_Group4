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
        public SpeakingAttempt GetOrCreateAttempt(int examId, int speakingId, int userId, string? audioUrl, string? transcript)
        {
            // 🔹 Tìm attempt bài thi hiện có
            var examAttempt = _context.ExamAttempt
                .FirstOrDefault(e => e.ExamId == examId && e.UserId == userId);

            if (examAttempt == null)
            {
                examAttempt = new ExamAttempt
                {
                    ExamId = examId,
                    UserId = userId,
                    StartedAt = DateTime.UtcNow,
                    SubmittedAt = DateTime.UtcNow
                };
                _context.ExamAttempt.Add(examAttempt);
                _context.SaveChanges();
            }

            // 🔹 Lấy attempt Speaking gần nhất
            var lastAttempt = _context.SpeakingAttempts
                .Where(a => a.AttemptId == examAttempt.AttemptId && a.SpeakingId == speakingId)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefault();

            // 🔸 Nếu audio mới hoặc chưa có -> tạo mới
            if (lastAttempt == null || !string.Equals(lastAttempt.AudioUrl, audioUrl, StringComparison.OrdinalIgnoreCase))
            {
                var newAttempt = new SpeakingAttempt
                {
                    AttemptId = examAttempt.AttemptId,  // ✅ dùng AttemptId, không phải ExamAttemptId
                    SpeakingId = speakingId,
                    AudioUrl = audioUrl,
                    Transcript = transcript,
                    StartedAt = DateTime.UtcNow,
                    SubmittedAt = DateTime.UtcNow
                };

                _context.SpeakingAttempts.Add(newAttempt);
                _context.SaveChanges();

                return newAttempt;
            }

            // 🔹 Nếu audio trùng -> dùng lại
            return lastAttempt;
        }



    }
}
