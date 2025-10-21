using System.Text.Json;
using WebAPI.Data;
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
        private readonly SpeechToTextService _speechToTextService;
        private readonly IExamService _examService;
        private readonly ApplicationDbContext _db;

        public SpeakingService(
            ISpeakingRepository speakingRepo,
            ISpeakingFeedbackRepository feedbackRepo,
            OpenAIService openAI,
            SpeechToTextService speechToTextService,
            IExamService examService,
            ApplicationDbContext db)
        {
            _speakingRepo = speakingRepo;
            _feedbackRepo = feedbackRepo;
            _openAI = openAI;
            _speechToTextService = speechToTextService;
            _examService = examService;
            _db = db;
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

            // 1️⃣ Convert speech to text using SpeechToTextService
            var tempAttemptId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var transcript = _speechToTextService.TranscribeAndSave(tempAttemptId, ans.AudioUrl ?? "");

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
                
                // 1️⃣ Convert speech to text using SpeechToTextService
                var tempAttemptId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var transcript = _speechToTextService.TranscribeAndSave(tempAttemptId, ans.AudioUrl ?? "");
                
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
                Console.WriteLine($"[SaveSpeakingFeedback] Starting save for examId: {examId}, speakingId: {speakingId}, userId: {userId}");
                
                // 1️⃣ Find or create ExamAttempt
                var attempts = _examService.GetExamAttemptsByUser(userId);
                var existingAttempt = attempts.FirstOrDefault(a => a.ExamId == examId);
                Console.WriteLine($"[SaveSpeakingFeedback] Found {attempts.Count()} attempts for user {userId}, existing for exam {examId}: {existingAttempt != null}");

                ExamAttempt attempt;
                if (existingAttempt == null)
                {
                    Console.WriteLine($"[SaveSpeakingFeedback] Creating new ExamAttempt for exam {examId}, user {userId}");
                    var dto = new SubmitAttemptDto
                    {
                        ExamId = examId,
                        AnswerText = JsonSerializer.Serialize(new { audioUrl, transcript }),
                        StartedAt = DateTime.UtcNow
                    };
                    attempt = _examService.SubmitAttempt(dto, userId);
                    Console.WriteLine($"[SaveSpeakingFeedback] Created ExamAttempt with ID: {attempt.AttemptId}");
                }
                else
                {
                    attempt = _examService.GetAttemptById(existingAttempt.AttemptId)
                              ?? throw new Exception("ExamAttempt not found.");
                    Console.WriteLine($"[SaveSpeakingFeedback] Using existing ExamAttempt with ID: {attempt.AttemptId}");

                    // update transcript/audio if empty
                    if (string.IsNullOrEmpty(attempt.AnswerText))
                    {
                        attempt.AnswerText = JsonSerializer.Serialize(new { audioUrl, transcript });
                        _examService.Save();
                    }
                }

                // 2️⃣ Extract band scores
                var band = feedback.RootElement.GetProperty("band_estimate");
                Console.WriteLine($"[SaveSpeakingFeedback] Extracted band scores: pronunciation={band.GetProperty("pronunciation").GetDecimal()}, overall={band.GetProperty("overall").GetDecimal()}");

                // 3️⃣ Tạo hoặc tìm SpeakingAttempt
                var speakingAttempt = _db.SpeakingAttempts
                    .FirstOrDefault(sa => sa.AttemptId == attempt.AttemptId && sa.SpeakingId == speakingId);

                if (speakingAttempt == null)
                {
                    Console.WriteLine($"[SaveSpeakingFeedback] Creating new SpeakingAttempt for attempt {attempt.AttemptId}, speaking {speakingId}");
                    speakingAttempt = new SpeakingAttempt
                    {
                        AttemptId = attempt.AttemptId,
                        SpeakingId = speakingId,
                        AudioUrl = audioUrl,
                        Transcript = transcript,
                        StartedAt = DateTime.UtcNow,
                        SubmittedAt = DateTime.UtcNow
                    };
                    _db.SpeakingAttempts.Add(speakingAttempt);
                    _db.SaveChanges();
                    Console.WriteLine($"[SaveSpeakingFeedback] Created SpeakingAttempt with ID: {speakingAttempt.SpeakingAttemptId}");
                }
                else
                {
                    Console.WriteLine($"[SaveSpeakingFeedback] Using existing SpeakingAttempt with ID: {speakingAttempt.SpeakingAttemptId}");
                    speakingAttempt.AudioUrl = audioUrl;
                    speakingAttempt.Transcript = transcript; // <<<<<< UPDATE TRANSCRIPT HERE TOO
                    speakingAttempt.SubmittedAt = DateTime.UtcNow; // Update submission time
                    _db.SpeakingAttempts.Update(speakingAttempt); // Mark as updated
                }
                _db.SaveChanges(); // Save changes for SpeakingAttempt
                Console.WriteLine($"[SaveSpeakingFeedback] Ensured SpeakingAttempt exists/created with ID: {speakingAttempt.SpeakingAttemptId}");
                // 4️⃣ Save or update SpeakingFeedback
                var oldFeedback = _feedbackRepo.GetAll()
                    .FirstOrDefault(f => f.SpeakingAttemptId == speakingAttempt.SpeakingAttemptId);

                if (oldFeedback != null)
                {
                    Console.WriteLine($"[SaveSpeakingFeedback] Updating existing feedback with ID: {oldFeedback.FeedbackId}");
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
                    Console.WriteLine($"[SaveSpeakingFeedback] Creating new SpeakingFeedback for SpeakingAttempt {speakingAttempt.SpeakingAttemptId}");
                    var entity = new SpeakingFeedback
                    {
                        SpeakingAttemptId = speakingAttempt.SpeakingAttemptId,
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
                Console.WriteLine($"[SaveSpeakingFeedback] Successfully saved feedback for exam {examId}, speaking {speakingId}, user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveSpeakingFeedback] Failed: {ex.Message}");
                Console.WriteLine($"[SaveSpeakingFeedback] Stack trace: {ex.StackTrace}");
                // Re-throw để caller biết có lỗi
                throw;
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
