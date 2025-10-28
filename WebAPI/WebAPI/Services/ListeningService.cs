using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebAPI.Services
{
    public class ListeningService : IListeningService
    {
        private readonly IListeningRepository _listeningRepo;

        public ListeningService(IListeningRepository listeningRepo)
        {
            _listeningRepo = listeningRepo;
        }

        public IReadOnlyList<Listening> GetListeningsByExam(int examId)
        {
            return _listeningRepo.GetByExamId(examId);
        }

        public decimal EvaluateListening(int examId, List<UserAnswerGroup> structuredAnswers)
        {
            var listenings = _listeningRepo.GetByExamId(examId);
            if (listenings == null || listenings.Count == 0)
                return 0m;

            int totalQuestions = 0;
            int correctCount = 0;

            // SkillId → normalized user answers
            var answerMap = structuredAnswers
                .Where(g => g.Answers != null && g.Answers.Count > 0)
                .ToDictionary(g => g.SkillId, g => g.ToNormalizedMap());

            foreach (var listening in listenings)
            {
                if (!answerMap.TryGetValue(listening.ListeningId, out var userMap))
                    continue;

                // ✅ Parse correct answers flexibly
                var correctMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                        listening.CorrectAnswer ?? "{}",
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (parsed != null)
                    {
                        foreach (var (key, je) in parsed)
                        {
                            switch (je.ValueKind)
                            {
                                case JsonValueKind.Array:
                                    correctMap[key] = je.EnumerateArray()
                                        .Select(x => x.GetString()?.Trim() ?? "")
                                        .Where(x => !string.IsNullOrWhiteSpace(x))
                                        .ToArray();
                                    break;

                                case JsonValueKind.String:
                                    var s = je.GetString()?.Trim();
                                    correctMap[key] = string.IsNullOrEmpty(s)
                                        ? Array.Empty<string>()
                                        : new[] { s };
                                    break;

                                default:
                                    correctMap[key] = new[] { je.ToString()?.Trim() ?? "" };
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ CorrectAnswer parse failed for listening {listening.ListeningId}: {ex.Message}");
                }

                // ✅ Evaluate answers
                foreach (var (questionKey, correctVals) in correctMap)
                {
                    totalQuestions++;

                    if (!userMap.TryGetValue(questionKey, out var userVals) || userVals.Length == 0)
                        continue;

                    var userSet = new HashSet<string>(
                        userVals.Select(v => v.Trim().ToLower()), StringComparer.OrdinalIgnoreCase);
                    var correctSet = new HashSet<string>(
                        correctVals.Select(v => v.Trim().ToLower()), StringComparer.OrdinalIgnoreCase);

                    if (userSet.SetEquals(correctSet))
                        correctCount++;
                }
            }

            if (totalQuestions == 0)
                return 0m;

            return Math.Round((decimal)correctCount / totalQuestions * 9, 1);
        }

        public IEnumerable<ListeningDto> GetAll()
        {
            var listenings = _listeningRepo.GetAll();
            return listenings.Select(r => new ListeningDto
            {
                ListeningId = r.ListeningId,
                ExamId = r.ExamId,
                ListeningContent = r.ListeningContent,
                ListeningQuestion = r.ListeningQuestion,
                ListeningType = r.ListeningType,
                DisplayOrder = r.DisplayOrder,
                CorrectAnswer = r.CorrectAnswer,
                QuestionHtml = r.QuestionHtml,
                CreatedAt = r.CreatedAt
            });
        }

        public ListeningDto? GetById(int id)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return null;

            return new ListeningDto
            {
                ListeningId = listening.ListeningId,
                ExamId = listening.ExamId,
                ListeningContent = listening.ListeningContent,
                ListeningQuestion = listening.ListeningQuestion,
                ListeningType = listening.ListeningType,
                DisplayOrder = listening.DisplayOrder,
                CorrectAnswer = listening.CorrectAnswer,
                QuestionHtml = listening.QuestionHtml,
                CreatedAt = listening.CreatedAt
            };
        }

        public ListeningDto? Add(CreateListeningDto dto)
        {
            var listening = new Listening
            {
                ExamId = dto.ExamId,
                ListeningContent = dto.ListeningContent,
                ListeningQuestion = dto.ListeningQuestion,
                ListeningType = dto.ListeningType ?? "Markdown",
                DisplayOrder = dto.DisplayOrder,
                CorrectAnswer = dto.CorrectAnswer,
                QuestionHtml = dto.QuestionHtml,
                CreatedAt = DateTime.UtcNow
            };

            _listeningRepo.Add(listening);
            _listeningRepo.SaveChanges();

            return new ListeningDto
            {
                ListeningId = listening.ListeningId,
                ExamId = listening.ExamId,
                ListeningContent = listening.ListeningContent,
                ListeningQuestion = listening.ListeningQuestion,
                ListeningType = listening.ListeningType,
                DisplayOrder = listening.DisplayOrder,
                CorrectAnswer = listening.CorrectAnswer,
                QuestionHtml = listening.QuestionHtml,
                CreatedAt = listening.CreatedAt
            };
        }

        public bool Update(int id, UpdateListeningDto dto)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return false;

            if (dto.ListeningContent != null)
                listening.ListeningContent = dto.ListeningContent;
            if (dto.ListeningQuestion != null)
                listening.ListeningQuestion = dto.ListeningQuestion;
            if (dto.ListeningType != null)
                listening.ListeningType = dto.ListeningType;
            if (dto.DisplayOrder.HasValue)
                listening.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.CorrectAnswer != null)
                listening.CorrectAnswer = dto.CorrectAnswer;
            if (dto.QuestionHtml != null)
                listening.QuestionHtml = dto.QuestionHtml;

            _listeningRepo.Update(listening);
            _listeningRepo.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var listening = _listeningRepo.GetById(id);
            if (listening == null) return false;

            _listeningRepo.Delete(listening);
            _listeningRepo.SaveChanges();
            return true;
        }
    }
}
