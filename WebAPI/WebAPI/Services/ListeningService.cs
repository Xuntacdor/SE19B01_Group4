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
            if (listenings == null || listenings.Count == 0) return 0m;

            int totalQuestions = 0;
            int correctAnswers = 0;

            var answerMap = structuredAnswers
                .Where(g => g.Answers != null)
                .ToDictionary(g => g.SkillId, g => g.Answers!);

            foreach (var r in listenings)
            {
                if (!answerMap.TryGetValue(r.ListeningId, out var userAnswers) || userAnswers.Count == 0)
                    continue;

                List<string> correctAnswersList;
                try
                {
                    if (r.CorrectAnswer?.TrimStart().StartsWith("[") == true)
                    {
                        correctAnswersList = JsonSerializer.Deserialize<List<string>>(r.CorrectAnswer!)?
                            .Select(a => a.Trim().ToLower()).ToList() ?? new();
                    }
                    else
                    {
                        correctAnswersList = (r.CorrectAnswer ?? "")
                            .Replace(" and ", ",")
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(a => a.Trim().ToLower())
                            .ToList();
                    }
                }
                catch
                {
                    correctAnswersList = new();
                }

                // Ensure we count all subquestions
                totalQuestions += correctAnswersList.Count;

                // Compare element-by-element (same order)
                for (int i = 0; i < correctAnswersList.Count; i++)
                {
                    if (i < userAnswers.Count &&
                        string.Equals(userAnswers[i].Trim(), correctAnswersList[i], StringComparison.OrdinalIgnoreCase))
                    {
                        correctAnswers++;
                    }
                }
            }

            // Scale to IELTS band (9)
            if (totalQuestions == 0) return 0m;
            return Math.Round((decimal)correctAnswers / totalQuestions * 9, 1);
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
