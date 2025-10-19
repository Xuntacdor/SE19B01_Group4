using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.ExternalServices;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class WritingService : IWritingService
    {
        private readonly IWritingRepository _writingRepo;
        private readonly IWritingFeedbackRepository _feedbackRepo;
        private readonly OpenAIService _openAI;
        private readonly IExamService _examService;

        public WritingService(
            IWritingRepository writingRepo,
            IWritingFeedbackRepository feedbackRepo,
            OpenAIService openAI,
            IExamService examService)
        {
            _writingRepo = writingRepo;
            _feedbackRepo = feedbackRepo;
            _openAI = openAI;
            _examService = examService;
        }

        // =====================
        // == CRUD METHODS ==
        // =====================
        public WritingDTO? GetById(int id)
        {
            var w = _writingRepo.GetById(id);
            return w == null ? null : MapToDto(w);
        }

        public List<WritingDTO> GetByExam(int examId)
        {
            return _writingRepo.GetByExamId(examId).Select(MapToDto).ToList();
        }

        public WritingDTO Create(WritingDTO dto)
        {
            var entity = new Writing
            {
                ExamId = dto.ExamId,
                WritingQuestion = dto.WritingQuestion,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = dto.ImageUrl
            };

            _writingRepo.Add(entity);
            _writingRepo.SaveChanges();
            return MapToDto(entity);
        }

        public WritingDTO? Update(int id, WritingDTO dto)
        {
            var existing = _writingRepo.GetById(id);
            if (existing == null) return null;

            existing.WritingQuestion = dto.WritingQuestion ?? existing.WritingQuestion;
            existing.DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : existing.DisplayOrder;
            existing.ImageUrl = dto.ImageUrl ?? existing.ImageUrl;

            _writingRepo.Update(existing);
            _writingRepo.SaveChanges();
            return MapToDto(existing);
        }

        public bool Delete(int id)
        {
            var existing = _writingRepo.GetById(id);
            if (existing == null) return false;
            _writingRepo.Delete(existing);
            _writingRepo.SaveChanges();
            return true;
        }

        // ======================
        // == GRADING LOGIC ==
        // ======================
        public JsonDocument GradeWriting(WritingGradeRequestDTO dto, int userId)
        {
            if (dto.Mode == "single")
            {
                var ans = dto.Answers.First();
                return GradeSingle(dto.ExamId, userId, ans);
            }
            else
            {
                return GradeFull(dto.ExamId, userId, dto.Answers);
            }
        }

        private JsonDocument GradeSingle(int examId, int userId, WritingAnswerDTO ans)
        {
            var question = _writingRepo.GetById(ans.WritingId)?.WritingQuestion ?? "Unknown question";
            var result = _openAI.GradeWriting(question, ans.AnswerText, ans.ImageUrl);

            SaveFeedback(examId, ans.WritingId, result, userId, ans.AnswerText);
            return result;
        }

        private JsonDocument GradeFull(int examId, int userId, List<WritingAnswerDTO> answers)
        {
            var feedbacks = new List<object>();

            foreach (var ans in answers)
            {
                var question = _writingRepo.GetById(ans.WritingId)?.WritingQuestion ?? "Unknown question";
                var result = _openAI.GradeWriting(question, ans.AnswerText, ans.ImageUrl);

                SaveFeedback(examId, ans.WritingId, result, userId, ans.AnswerText);

                feedbacks.Add(new
                {
                    writingId = ans.WritingId,
                    displayOrder = ans.DisplayOrder,
                    feedback = JsonSerializer.Deserialize<object>(result.RootElement.GetRawText())
                });
            }

            var response = new
            {
                message = "Full writing test graded successfully.",
                examId,
                totalAnswers = answers.Count,
                feedbacks
            };

            return JsonDocument.Parse(JsonSerializer.Serialize(response));
        }


        private void SaveFeedback(int examId, int writingId, JsonDocument feedback, int userId, string answerText)
        {
            try
            {
                // 1️⃣ Tìm ExamAttempt hiện tại (nếu chưa có thì tạo mới)
                var attemptList = _examService.GetExamAttemptsByUser(userId);
                var attemptSummary = attemptList.FirstOrDefault(a => a.ExamId == examId);

                ExamAttempt attempt;

                if (attemptSummary == null)
                {
                    var dto = new SubmitAttemptDto
                    {
                        ExamId = examId,
                        AnswerText = answerText,
                        StartedAt = DateTime.UtcNow
                    };

                    attempt = _examService.SubmitAttempt(dto, userId);
                }
                else
                {
                    attempt = _examService.GetAttemptById(attemptSummary.AttemptId)
                              ?? throw new Exception("ExamAttempt not found in database.");

                    if (string.IsNullOrEmpty(attempt.AnswerText))
                    {
                        attempt.AnswerText = answerText;
                        _examService.Save();
                    }
                }

                var band = feedback.RootElement.GetProperty("band_estimate");

                // 3️⃣ Kiểm tra xem feedback cũ đã tồn tại chưa
                var existing = _feedbackRepo.GetAll()
                    .FirstOrDefault(f => f.AttemptId == attempt.AttemptId && f.WritingId == writingId);

                if (existing != null)
                {
                    // 4️⃣ Ghi đè (update) bài chấm cũ
                    existing.TaskAchievement = band.GetProperty("task_achievement").GetDecimal();
                    // Map organization_logic từ JSON thành coherence_cohesion trong database
                    existing.CoherenceCohesion = band.GetProperty("organization_logic").GetDecimal();
                    existing.LexicalResource = band.GetProperty("lexical_resource").GetDecimal();
                    existing.GrammarAccuracy = band.GetProperty("grammar_accuracy").GetDecimal();
                    existing.Overall = band.GetProperty("overall").GetDecimal();
                    existing.GrammarVocabJson = feedback.RootElement.GetProperty("grammar_vocab").GetRawText();
                    existing.FeedbackSections = feedback.RootElement.GetProperty("overall_feedback").GetRawText();
                    existing.CreatedAt = DateTime.UtcNow;

                    _feedbackRepo.Update(existing);
                }
                else
                {
                    var entity = new WritingFeedback
                    {
                        AttemptId = attempt.AttemptId,
                        WritingId = writingId,
                        TaskAchievement = band.GetProperty("task_achievement").GetDecimal(),
                        // Map organization_logic từ JSON thành coherence_cohesion trong database
                        CoherenceCohesion = band.GetProperty("organization_logic").GetDecimal(),
                        LexicalResource = band.GetProperty("lexical_resource").GetDecimal(),
                        GrammarAccuracy = band.GetProperty("grammar_accuracy").GetDecimal(),
                        Overall = band.GetProperty("overall").GetDecimal(),
                        GrammarVocabJson = feedback.RootElement.GetProperty("grammar_vocab").GetRawText(),
                        FeedbackSections = feedback.RootElement.GetProperty("overall_feedback").GetRawText(),
                        CreatedAt = DateTime.UtcNow
                    };

                    _feedbackRepo.Add(entity);
                }

                _feedbackRepo.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveFeedback] Failed: {ex.Message}");
            }
        }


        private static WritingDTO MapToDto(Writing w) =>
            new WritingDTO
            {
                WritingId = w.WritingId,
                ExamId = w.ExamId,
                WritingQuestion = w.WritingQuestion,
                DisplayOrder = w.DisplayOrder,
                CreatedAt = w.CreatedAt,
                ImageUrl = w.ImageUrl
            };
    }
}
