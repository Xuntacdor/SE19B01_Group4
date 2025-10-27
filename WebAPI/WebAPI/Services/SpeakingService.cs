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
        private readonly SpeechToTextService _speechToTextService;
        private readonly IExamService _examService;

        public SpeakingService(
            ISpeakingRepository speakingRepo,
            ISpeakingFeedbackRepository feedbackRepo,
            OpenAIService openAI,
            SpeechToTextService speechToTextService,
            IExamService examService)
        {
            _speakingRepo = speakingRepo;
            _feedbackRepo = feedbackRepo;
            _openAI = openAI;
            _speechToTextService = speechToTextService;
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
            return _speakingRepo.Delete(id);
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
            var tempAttemptId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Convert audio to transcript
            var transcript = _speechToTextService.TranscribeAndSave(tempAttemptId, ans.AudioUrl ?? "");

            // Grade with AI
            var result = _openAI.GradeSpeaking(question, transcript);

            // Save feedback using repository
            _feedbackRepo.SaveFeedback(examId, ans.SpeakingId, result, userId, ans.AudioUrl, transcript);

            return result;
        }

        private JsonDocument GradeFull(int examId, int userId, List<SpeakingAnswerDTO> answers)
        {
            var feedbacks = new List<object>();

            foreach (var ans in answers)
            {
                var question = _speakingRepo.GetById(ans.SpeakingId)?.SpeakingQuestion ?? "Unknown question";
                var tempAttemptId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Convert audio to transcript
                var transcript = _speechToTextService.TranscribeAndSave(tempAttemptId, ans.AudioUrl ?? "");

                // Grade each question
                var result = _openAI.GradeSpeaking(question, transcript);

                // Save feedback via repository
                _feedbackRepo.SaveFeedback(examId, ans.SpeakingId, result, userId, ans.AudioUrl, transcript);

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
        // == DTO MAPPING ==
        // ===========================
        private static SpeakingDTO MapToDto(Speaking s) => new SpeakingDTO
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
