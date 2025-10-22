using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WebAPI.Services
{
    public class ReadingService : IReadingService
    {
        private readonly IReadingRepository _readingRepo;

        public ReadingService(IReadingRepository readingRepo)
        {
            _readingRepo = readingRepo;
        }

        public IReadOnlyList<Reading> GetReadingsByExam(int examId)
        {
            return _readingRepo.GetByExamId(examId);
        }

        public decimal EvaluateReading(int examId, List<UserAnswerGroup> structuredAnswers)
        {
            var readings = _readingRepo.GetByExamId(examId);
            if (readings == null || readings.Count == 0) return 0m;

            int totalQuestions = 0;
            int correctAnswers = 0;

            var answerMap = structuredAnswers
                .Where(g => g.Answers != null)
                .ToDictionary(g => g.SkillId, g => g.Answers!);

            foreach (var r in readings)
            {
                if (!answerMap.TryGetValue(r.ReadingId, out var userAnswers) || userAnswers.Count == 0)
                    continue;

                // Parse DB correct answers (supports JSON or plain text)
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


        public IEnumerable<ReadingDto> GetAll()
        {
            var readings = _readingRepo.GetAll();
            return readings.Select(r => new ReadingDto
            {
                ReadingId = r.ReadingId,
                ExamId = r.ExamId,
                ReadingContent = r.ReadingContent,
                ReadingQuestion = r.ReadingQuestion,
                ReadingType = r.ReadingType,
                DisplayOrder = r.DisplayOrder,
                CorrectAnswer = r.CorrectAnswer,
                QuestionHtml = r.QuestionHtml,
                CreatedAt = r.CreatedAt
            });
        }

        public ReadingDto? GetById(int id)
        {
            var reading = _readingRepo.GetById(id);
            if (reading == null) return null;

            return new ReadingDto
            {
                ReadingId = reading.ReadingId,
                ExamId = reading.ExamId,
                ReadingContent = reading.ReadingContent,
                ReadingQuestion = reading.ReadingQuestion,
                ReadingType = reading.ReadingType,
                DisplayOrder = reading.DisplayOrder,
                CorrectAnswer = reading.CorrectAnswer,
                QuestionHtml = reading.QuestionHtml,
                CreatedAt = reading.CreatedAt
            };
        }

        public ReadingDto? Add(CreateReadingDto dto)
        {
            var reading = new Reading
            {
                ExamId = dto.ExamId,
                ReadingContent = dto.ReadingContent,
                ReadingQuestion = dto.ReadingQuestion,
                ReadingType = dto.ReadingType ?? "Markdown",
                DisplayOrder = dto.DisplayOrder,
                CorrectAnswer = dto.CorrectAnswer,
                QuestionHtml = dto.QuestionHtml,
                CreatedAt = DateTime.UtcNow
            };

            _readingRepo.Add(reading);
            _readingRepo.SaveChanges();

            return new ReadingDto
            {
                ReadingId = reading.ReadingId,
                ExamId = reading.ExamId,
                ReadingContent = reading.ReadingContent,
                ReadingQuestion = reading.ReadingQuestion,
                ReadingType = reading.ReadingType,
                DisplayOrder = reading.DisplayOrder,
                CorrectAnswer = reading.CorrectAnswer,
                QuestionHtml = reading.QuestionHtml,
                CreatedAt = reading.CreatedAt
            };
        }

        public bool Update(int id, UpdateReadingDto dto)
        {
            var reading = _readingRepo.GetById(id);
            if (reading == null) return false;

            if (dto.ReadingContent != null)
                reading.ReadingContent = dto.ReadingContent;
            if (dto.ReadingQuestion != null)
                reading.ReadingQuestion = dto.ReadingQuestion;
            if (dto.ReadingType != null)
                reading.ReadingType = dto.ReadingType;
            if (dto.DisplayOrder.HasValue)
                reading.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.CorrectAnswer != null)
                reading.CorrectAnswer = dto.CorrectAnswer;
            if (dto.QuestionHtml != null)
                reading.QuestionHtml = dto.QuestionHtml;

            _readingRepo.Update(reading);
            _readingRepo.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var reading = _readingRepo.GetById(id);
            if (reading == null) return false;

            _readingRepo.Delete(reading);
            _readingRepo.SaveChanges();
            return true;
        }
    }
}
