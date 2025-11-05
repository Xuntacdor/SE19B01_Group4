using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebAPI.Data;
using WebAPI.Models;

namespace WebAPI.Repositories
{
    public class SpeakingFeedbackRepository : ISpeakingFeedbackRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ISpeakingRepository _speakingRepo;

        public SpeakingFeedbackRepository(ApplicationDbContext context, ISpeakingRepository speakingRepo)
        {
            _context = context;
            _speakingRepo = speakingRepo;
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
                .Where(f =>
                    f.SpeakingAttempt != null &&
                    f.SpeakingAttempt.ExamAttempt != null &&
                    f.SpeakingAttempt.SpeakingId == speakingId &&
                    f.SpeakingAttempt.ExamAttempt.UserId == userId)
                .OrderByDescending(f => f.CreatedAt) // ✅ lấy feedback mới nhất
                .FirstOrDefault();
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

        // ✅ Fixed version with FK-safe save
        public void SaveFeedback(int examId, int speakingId, JsonDocument feedback, int userId, string? audioUrl, string? transcript)
        {
            // 1️⃣ Get or create SpeakingAttempt (ensures FK is valid)
            var attempt = _speakingRepo.GetOrCreateAttempt(examId, speakingId, userId, audioUrl, transcript);

            var band = feedback.RootElement.GetProperty("band_estimate");

            // 2️⃣ Try find existing feedback for this attempt
            var existing = _context.SpeakingFeedbacks
                .FirstOrDefault(f => f.SpeakingAttemptId == attempt.SpeakingAttemptId);

            if (existing != null)
            {
                existing.Pronunciation = band.GetProperty("pronunciation").GetDecimal();
                existing.Fluency = band.GetProperty("fluency").GetDecimal();
                existing.LexicalResource = band.GetProperty("lexical_resource").GetDecimal();
                existing.GrammarAccuracy = band.GetProperty("grammar_accuracy").GetDecimal();
                existing.Coherence = band.GetProperty("coherence").GetDecimal();
                existing.Overall = band.GetProperty("overall").GetDecimal();
                existing.AiAnalysisJson = feedback.RootElement.GetRawText();
                existing.CreatedAt = DateTime.UtcNow;
                _context.SpeakingFeedbacks.Update(existing);
            }
            else
            {
                var entity = new SpeakingFeedback
                {
                    SpeakingAttemptId = attempt.SpeakingAttemptId, 
                    Pronunciation = band.GetProperty("pronunciation").GetDecimal(),
                    Fluency = band.GetProperty("fluency").GetDecimal(),
                    LexicalResource = band.GetProperty("lexical_resource").GetDecimal(),
                    GrammarAccuracy = band.GetProperty("grammar_accuracy").GetDecimal(),
                    Coherence = band.GetProperty("coherence").GetDecimal(),
                    Overall = band.GetProperty("overall").GetDecimal(),
                    AiAnalysisJson = feedback.RootElement.GetRawText(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.SpeakingFeedbacks.Add(entity);
            }

            _context.SaveChanges();
        }
    }
}
