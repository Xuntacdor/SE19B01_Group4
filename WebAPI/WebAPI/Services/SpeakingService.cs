using System.Text.Json;
using WebAPI.DTOs;
using WebAPI.ExternalServices;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class SpeakingService : ISpeakingService
    {
        private readonly ISpeakingRepository _speakingRepo;
        private readonly ISpeakingFeedbackRepository _feedbackRepo;
        private readonly OpenAIService _openAI;
        private readonly IExamService _examService;

        public SpeakingService(
            ISpeakingRepository speakingRepo,
            ISpeakingFeedbackRepository feedbackRepo,
            OpenAIService openAI,
            IExamService examService)
        {
            _speakingRepo = speakingRepo;
            _feedbackRepo = feedbackRepo;
            _openAI = openAI;
            _examService = examService;
        }

        // ===========================
        // == CRUD METHODS ==
        // ===========================
        public SpeakingDTO? GetById(int id)
        {
            var s = _speakingRepo.GetById(id);
            return s == null ? null : MapToDto(s);
        }

        public List<SpeakingDTO> GetByExam(int examId)
        {
            return _speakingRepo.GetByExamId(examId).Select(MapToDto).ToList();
        }

        public SpeakingDTO Create(SpeakingDTO dto)
        {
            var entity = new Speaking
            {
                ExamId = dto.ExamId,
                SpeakingQuestion = dto.SpeakingQuestion,
                SpeakingType = dto.SpeakingType,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = DateTime.UtcNow
            };

            _speakingRepo.Add(entity);
            _speakingRepo.SaveChanges();
            return MapToDto(entity);
        }

        public SpeakingDTO? Update(int id, SpeakingDTO dto)
        {
            var existing = _speakingRepo.GetById(id);
            if (existing == null) return null;

            existing.SpeakingQuestion = dto.SpeakingQuestion ?? existing.SpeakingQuestion;
            existing.SpeakingType = dto.SpeakingType ?? existing.SpeakingType;
            existing.DisplayOrder = dto.DisplayOrder > 0 ? dto.DisplayOrder : existing.DisplayOrder;

            _speakingRepo.Update(existing);
            _speakingRepo.SaveChanges();
            return MapToDto(existing);
        }

        public bool Delete(int id)
        {
            var existing = _speakingRepo.GetById(id);
            if (existing == null) return false;
            _speakingRepo.Delete(existing);
            _speakingRepo.SaveChanges();
            return true;
        }

        // ===========================
        // == GRADING LOGIC ==
        // ===========================
        public JsonDocument GradeSpeaking(SpeakingGradeRequestDTO dto, int userId)
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

        private JsonDocument GradeSingle(int examId, int userId, SpeakingAnswerDTO ans)
        {
            var question = _speakingRepo.GetById(ans.SpeakingId)?.SpeakingQuestion ?? "Unknown question";

            // 1️⃣ Convert speech to text
            var transcript = _openAI.SpeechToText(ans.AudioUrl ?? "");

            // 2️⃣ Grade speaking using transcript only
            var result = _openAI.GradeSpeaking(question, transcript);

            // 3️⃣ Save feedback to DB
            SaveFeedback(examId, ans.SpeakingId, result, userId, ans.AudioUrl, transcript);

            return result;
        }

        private JsonDocument GradeFull(int examId, int userId, List<SpeakingAnswerDTO> answers)
        {
            var feedbacks = new List<object>();

            foreach (var ans in answers)
            {
                var question = _speakingRepo.GetById(ans.SpeakingId)?.SpeakingQuestion ?? "Unknown question";
                var transcript = _openAI.SpeechToText(ans.AudioUrl ?? "");
                var result = _openAI.GradeSpeaking(question, transcript);

                SaveFeedback(examId, ans.SpeakingId, result, userId, ans.AudioUrl, transcript);

                feedbacks.Add(new
                {
                    speakingId = ans.SpeakingId,
                    displayOrder = ans.DisplayOrder,
                    feedback = JsonSerializer.Deserialize<object>(result.RootElement.GetRawText())
                });
            }

            var response = new
            {
                message = "Full speaking test graded successfully.",
                examId,
                totalAnswers = answers.Count,
                feedbacks
            };

            return JsonDocument.Parse(JsonSerializer.Serialize(response));
        }

        // ===========================
        // == SAVE FEEDBACK ==
        // ===========================
        private void SaveFeedback(int examId, int speakingId, JsonDocument feedback, int userId, string? audioUrl, string? transcript)
        {
            try
            {
                // 1️⃣ Find or create ExamAttempt
                var attempts = _examService.GetExamAttemptsByUser(userId);
                var existingAttempt = attempts.FirstOrDefault(a => a.ExamId == examId);

                ExamAttempt attempt;
                if (existingAttempt == null)
                {
                    var dto = new SubmitAttemptDto
                    {
                        ExamId = examId,
                        AnswerText = JsonSerializer.Serialize(new { audioUrl, transcript }),
                        StartedAt = DateTime.UtcNow
                    };
                    attempt = _examService.SubmitAttempt(dto, userId);
                }
                else
                {
                    attempt = _examService.GetAttemptById(existingAttempt.AttemptId)
                              ?? throw new Exception("ExamAttempt not found.");

                    // update transcript/audio if empty
                    if (string.IsNullOrEmpty(attempt.AnswerText))
                    {
                        attempt.AnswerText = JsonSerializer.Serialize(new { audioUrl, transcript });
                        _examService.Save();
                    }
                }

                // 2️⃣ Extract band scores
                var band = feedback.RootElement.GetProperty("band_estimate");

                // 3️⃣ Save or update SpeakingFeedback
                var oldFeedback = _feedbackRepo.GetAll()
                    .FirstOrDefault(f => f.AttemptId == attempt.AttemptId && f.SpeakingId == speakingId);

                if (oldFeedback != null)
                {
                    oldFeedback.Pronunciation = band.GetProperty("pronunciation").GetDecimal();
                    oldFeedback.Fluency = band.GetProperty("fluency").GetDecimal();
                    oldFeedback.LexicalResource = band.GetProperty("lexical_resource").GetDecimal();
                    oldFeedback.GrammarAccuracy = band.GetProperty("grammar_accuracy").GetDecimal();
                    oldFeedback.Coherence = band.GetProperty("coherence").GetDecimal();
                    oldFeedback.Overall = band.GetProperty("overall").GetDecimal();
                    oldFeedback.AiAnalysisJson = feedback.RootElement.GetRawText();
                    oldFeedback.CreatedAt = DateTime.UtcNow;

                    _feedbackRepo.Update(oldFeedback);
                }
                else
                {
                    var entity = new SpeakingFeedback
                    {
                        AttemptId = attempt.AttemptId,
                        SpeakingId = speakingId,
                        Pronunciation = band.GetProperty("pronunciation").GetDecimal(),
                        Fluency = band.GetProperty("fluency").GetDecimal(),
                        LexicalResource = band.GetProperty("lexical_resource").GetDecimal(),
                        GrammarAccuracy = band.GetProperty("grammar_accuracy").GetDecimal(),
                        Coherence = band.GetProperty("coherence").GetDecimal(),
                        Overall = band.GetProperty("overall").GetDecimal(),
                        AiAnalysisJson = feedback.RootElement.GetRawText(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _feedbackRepo.Add(entity);
                }

                _feedbackRepo.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveSpeakingFeedback] Failed: {ex.Message}");
            }
        }

        private static SpeakingDTO MapToDto(Speaking s) =>
            new SpeakingDTO
            {
                SpeakingId = s.SpeakingId,
                ExamId = s.ExamId,
                SpeakingQuestion = s.SpeakingQuestion,
                SpeakingType = s.SpeakingType,
                DisplayOrder = s.DisplayOrder,
                CreatedAt = s.CreatedAt
            };
    }
}
