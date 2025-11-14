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
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefault();
        }
        public SpeakingFeedback GetBySpeakingAttemptAndUser(int speakingAttemptId, int userId)
        {
            return _context.SpeakingFeedbacks
                .Include(f => f.SpeakingAttempt)
                .ThenInclude(a => a.ExamAttempt)
                .FirstOrDefault(f =>
                    f.SpeakingAttemptId == speakingAttemptId &&
                    f.SpeakingAttempt.ExamAttempt.UserId == userId
                );
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
        decimal CalculateIeltsSpeakingBand(decimal pr, decimal fl, decimal lr, decimal gr)
        {
            var avg = (pr + fl + lr + gr) / 4;

           
            var rounded = Math.Round(avg * 2, MidpointRounding.AwayFromZero) / 2;
            return rounded;
        }



        public void SaveFeedback(int examId, int speakingId, JsonDocument feedback, int userId, string? audioUrl, string? transcript)
        {
            // 1️⃣ Get or create SpeakingAttempt (ensures FK is valid)
            var attempt = _speakingRepo.GetOrCreateAttempt(examId, speakingId, userId, audioUrl, transcript);

            // 2️⃣ Extract AI band values
            var band = feedback.RootElement.GetProperty("band_estimate");

            var pr = band.GetProperty("pronunciation").GetDecimal();
            var fl = band.GetProperty("fluency").GetDecimal();
            var lr = band.GetProperty("lexical_resource").GetDecimal();
            var gr = band.GetProperty("grammar_accuracy").GetDecimal();

            // IELTS: Coherence = Fluency
            var coherence = fl;

            // 3️⃣ Compute IELTS-standard overall
            var overall = CalculateIeltsSpeakingBand(pr, fl, lr, gr);

            // 4️⃣ Check if feedback exists
            var existing = _context.SpeakingFeedbacks
                .FirstOrDefault(f => f.SpeakingAttemptId == attempt.SpeakingAttemptId);

            if (existing != null)
            {
                // === UPDATE ===
                existing.Pronunciation = pr;
                existing.Fluency = fl;
                existing.LexicalResource = lr;
                existing.GrammarAccuracy = gr;
                existing.Coherence = coherence;
                existing.Overall = overall;

                existing.AiAnalysisJson = feedback.RootElement.GetRawText();
                existing.CreatedAt = DateTime.UtcNow;

                _context.SpeakingFeedbacks.Update(existing);
            }
            else
            {
                // === INSERT ===
                var entity = new SpeakingFeedback
                {
                    SpeakingAttemptId = attempt.SpeakingAttemptId,
                    Pronunciation = pr,
                    Fluency = fl,
                    LexicalResource = lr,
                    GrammarAccuracy = gr,
                    Coherence = coherence,
                    Overall = overall,
                    AiAnalysisJson = feedback.RootElement.GetRawText(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.SpeakingFeedbacks.Add(entity);
            }

            _context.SaveChanges();
        }

    }
}
