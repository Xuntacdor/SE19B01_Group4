using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class SpeakingFeedbackRepository : ISpeakingFeedbackRepository
    {
        private readonly ApplicationDbContext _context;
        public SpeakingFeedbackRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<SpeakingFeedback> GetAll()
        {
            return _context.SpeakingFeedbacks
                .Include(f => f.SpeakingAttempt)
                    .ThenInclude(sa => sa.ExamAttempt)
                .ToList();
        }

        public SpeakingFeedback? GetById(int id)
        {
            return _context.SpeakingFeedbacks
                .Include(f => f.SpeakingAttempt)
                    .ThenInclude(sa => sa.ExamAttempt)
                .FirstOrDefault(f => f.FeedbackId == id);
        }

        public List<SpeakingFeedback> GetByExamAndUser(int examId, int userId)
        {
            return _context.SpeakingFeedbacks
                .Include(f => f.SpeakingAttempt)
                    .ThenInclude(sa => sa.ExamAttempt)
                .Where(f => f.SpeakingAttempt != null &&
                            f.SpeakingAttempt.ExamAttempt != null &&
                            f.SpeakingAttempt.ExamAttempt.ExamId == examId &&
                            f.SpeakingAttempt.ExamAttempt.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
        }

        public SpeakingFeedback? GetBySpeakingAndUser(int speakingId, int userId)
        {
            return _context.SpeakingFeedbacks
                .Include(f => f.SpeakingAttempt)
                    .ThenInclude(sa => sa.ExamAttempt)
                .FirstOrDefault(f =>
                    f.SpeakingAttempt != null &&
                    f.SpeakingAttempt.ExamAttempt != null &&
                    f.SpeakingAttempt.SpeakingId == speakingId &&
                    f.SpeakingAttempt.ExamAttempt.UserId == userId);
        }


        public void Add(SpeakingFeedback entity)
        {
            _context.SpeakingFeedbacks.Add(entity);
        }

        public void Update(SpeakingFeedback entity)
        {
            _context.SpeakingFeedbacks.Update(entity);
        }

        public void Delete(SpeakingFeedback entity)
        {
            _context.SpeakingFeedbacks.Remove(entity);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}
