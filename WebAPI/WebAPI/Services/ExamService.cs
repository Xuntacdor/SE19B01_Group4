using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class ExamService : IExamService
    {
        private readonly IExamRepository _repo;
        private readonly IExamAttemptRepository _attemptRepo;

        public ExamService(IExamRepository repo, IExamAttemptRepository attemptRepo)
        {
            _repo = repo;
            _attemptRepo = attemptRepo;
        }

        // ===== Exams =====
        public Exam? GetById(int id) => _repo.GetById(id);
        public List<Exam> GetAll() => _repo.GetAll();

        public Exam Create(CreateExamDto dto)
        {
            var exam = new Exam
            {
                ExamName = dto.ExamName,
                ExamType = dto.ExamType
            };

            _repo.Add(exam);
            _repo.SaveChanges();
            return exam;
        }

        public Exam? Update(int id, UpdateExamDto dto)
        {
            var existing = _repo.GetById(id);
            if (existing == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.ExamName))
                existing.ExamName = dto.ExamName;
            if (!string.IsNullOrWhiteSpace(dto.ExamType))
                existing.ExamType = dto.ExamType;

            _repo.Update(existing);
            _repo.SaveChanges();
            return existing;
        }

        public bool Delete(int id)
        {
            var exam = _repo.GetById(id);
            if (exam == null) return false;

            _repo.Delete(exam);
            _repo.SaveChanges();
            return true;
        }

        // ===== Attempts =====
        public ExamAttempt SubmitAttempt(SubmitAttemptDto dto, int userId)
        {
            var exam = _repo.GetById(dto.ExamId);
            if (exam == null)
                throw new KeyNotFoundException("Exam not found");

            var attempt = new ExamAttempt
            {
                ExamId = dto.ExamId,
                UserId = userId,
                StartedAt = dto.StartedAt == default ? DateTime.UtcNow : dto.StartedAt,
                SubmittedAt = DateTime.UtcNow,
                AnswerText = dto.AnswerText,
                Score = dto.Score
            };

            _attemptRepo.Add(attempt);
            _attemptRepo.SaveChanges();

            return attempt;
        }

        public ExamAttempt? GetAttemptById(long attemptId) => _attemptRepo.GetById((int)attemptId);

        public List<ExamAttemptSummaryDto> GetExamAttemptsByUser(int userId) =>
            _repo.GetExamAttemptsByUser(userId);

        public ExamAttemptDto? GetExamAttemptDetail(long attemptId) =>
            _repo.GetExamAttemptDetail(attemptId);

        public void Save()
        {
            _repo.SaveChanges();
            _attemptRepo.SaveChanges();
        }
    }
}
