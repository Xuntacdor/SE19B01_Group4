using System.Collections.Generic;
using System.Linq;
using WebAPI.Data;
using WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Repositories
{
    public class WritingFeedbackRepository : IWritingFeedbackRepository
    {
        private readonly ApplicationDbContext _db;

        public WritingFeedbackRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<WritingFeedback> GetAll()
        {
            return _db.WritingFeedback
                .Include(f => f.ExamAttempt)
                .ToList();
        }

        public WritingFeedback? GetById(int id)
        {
            return _db.WritingFeedback
                .Include(f => f.ExamAttempt)
                .FirstOrDefault(f => f.FeedbackId == id);
        }

        public List<WritingFeedback> GetByExamAndUser(int examId, int userId)
        {
            return _db.WritingFeedback
                .Include(f => f.ExamAttempt)
                .Where(f => f.ExamAttempt != null &&
                            f.ExamAttempt.ExamId == examId &&
                            f.ExamAttempt.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
        }

        // ✅ Thêm mới hàm tiện ích cho ghi đè feedback
        public WritingFeedback? GetByAttemptAndWriting(long attemptId, int writingId)
        {
            return _db.WritingFeedback
                .FirstOrDefault(f => f.AttemptId == attemptId && f.WritingId == writingId);
        }

        public void Add(WritingFeedback entity)
        {
            _db.WritingFeedback.Add(entity);
        }

        public void Update(WritingFeedback entity)
        {
            _db.WritingFeedback.Update(entity);
        }

        public void Delete(WritingFeedback entity)
        {
            _db.WritingFeedback.Remove(entity);
        }

        public void SaveChanges()
        {
            _db.SaveChanges();
        }
    }
}
