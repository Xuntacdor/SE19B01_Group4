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
        private readonly IOpenAIService _openAI;
        private readonly IExamService _examService;

        public WritingService(
            IWritingRepository writingRepo,
            IWritingFeedbackRepository feedbackRepo,
            IOpenAIService openAI, 
            IExamService examService)
        {
            _writingRepo = writingRepo;
            _feedbackRepo = feedbackRepo;
            _openAI = openAI;
            _examService = examService;
        }
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
            var result = _openAI.GradeWriting(question, ans.AnswerText, ans.ImageUrl); // ✅ Interface

            SaveFeedback(examId, ans.WritingId, result, userId, ans.AnswerText);
            return result;
        }

        private JsonDocument GradeFull(int examId, int userId, List<WritingAnswerDTO> answers)
        {
            var feedbacks = new List<object>();

            foreach (var ans in answers)
            {
                var question = _writingRepo.GetById(ans.WritingId)?.WritingQuestion ?? "Unknown question";
                var result = _openAI.GradeWriting(question, ans.AnswerText, ans.ImageUrl); // ✅ Interface

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
                // Lấy attempt hiện tại (nếu có)
                var attemptList = _examService.GetExamAttemptsByUser(userId);
                var attemptSummary = attemptList.FirstOrDefault(a => a.ExamId == examId);

                ExamAttempt attempt;

                if (attemptSummary == null)
                {
                    // Nếu chưa có attempt → tạo mới
                    var dto = new SubmitAttemptDto
                    {
                        ExamId = examId,
                        AnswerText = "", // 🔥 ban đầu rỗng
                        StartedAt = DateTime.UtcNow
                    };

                    attempt = _examService.SubmitAttempt(dto, userId);
                }
                else
                {
                    // Nếu attempt tồn tại → luôn ghi đè toàn bộ
                    attempt = _examService.GetAttemptById(attemptSummary.AttemptId)
                              ?? throw new Exception("ExamAttempt not found in database.");
                }

                /*
                    🔥 REPLACE TOÀN BỘ ANSWER TEXT CHO MỖI VÒNG CHẤM FULL
                    Mình không append ở đây.
                    GradeFull sẽ gọi hàm này 2 lần (task1, task2).
                    Bài nào gọi sau sẽ đè bài trước → TAO KHÔNG MUỐN THẾ.

                    => Ghi đè logic phải đặt ở GRADEFULL, không đặt ở đây.
                */

                // --------- CHỈ GHI ĐÈ THEO TASK CỤ THỂ ----------
                var newTaskBlock = $"--- TASK {writingId} ---\n{answerText}";

                // Nếu đây là task 1 (displayOrder = 1) → reset toàn bộ answerText
                // để chuẩn bị ghi lại từ đầu
                if (writingId == 10 || writingId == 1) // tuỳ ID task 1 của m
                {
                    attempt.AnswerText = newTaskBlock;
                }
                else
                {
                    // Task 2 → append sau khi task 1 đã reset
                    attempt.AnswerText += "\n\n" + newTaskBlock;
                }

                _examService.Save();

                // =============================== FEEDBACK ===============================
                var band = feedback.RootElement.GetProperty("band_estimate");
                var existing = _feedbackRepo.GetByAttemptAndWriting(attempt.AttemptId, writingId);

                if (existing != null)
                {
                    existing.TaskAchievement = band.GetProperty("task_achievement").GetDecimal();
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
