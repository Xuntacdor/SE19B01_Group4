using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Services
{
    public class ListeningService : IListeningService
    {
        private readonly IListeningRepository _ListeningRepo;

        public ListeningService(IListeningRepository ListeningRepo)
        {
            _ListeningRepo = ListeningRepo;
        }

        public IReadOnlyList<Listening> GetListeningsByExam(int examId)
        {
            return _ListeningRepo.GetByExamId(examId);
        }

        public decimal EvaluateListening(int examId, IDictionary<int, string> answers)
        {
            var Listenings = _ListeningRepo.GetByExamId(examId);
            if (Listenings == null || Listenings.Count == 0) return 0m;

            int correct = 0;
            foreach (var r in Listenings)
            {
                if (answers.TryGetValue(r.ListeningId, out string? userAnswer) &&
                    string.Equals(r.CorrectAnswer, userAnswer, StringComparison.OrdinalIgnoreCase))
                {
                    correct++;
                }
            }

            return Math.Round((decimal)correct / Listenings.Count * 9, 1);
        }

        public IEnumerable<ListeningDto> GetAll()
        {
            var Listenings = _ListeningRepo.GetAll();
            return Listenings.Select(r => new ListeningDto
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
            var Listening = _ListeningRepo.GetById(id);
            if (Listening == null) return null;

            return new ListeningDto
            {
                ListeningId = Listening.ListeningId,
                ExamId = Listening.ExamId,
                ListeningContent = Listening.ListeningContent,
                ListeningQuestion = Listening.ListeningQuestion,
                ListeningType = Listening.ListeningType,
                DisplayOrder = Listening.DisplayOrder,
                CorrectAnswer = Listening.CorrectAnswer,
                QuestionHtml = Listening.QuestionHtml,
                CreatedAt = Listening.CreatedAt
            };
        }

        public ListeningDto? Add(CreateListeningDto dto)
        {
            var Listening = new Listening
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

            _ListeningRepo.Add(Listening);
            _ListeningRepo.SaveChanges();

            return new ListeningDto
            {
                ListeningId = Listening.ListeningId,
                ExamId = Listening.ExamId,
                ListeningContent = Listening.ListeningContent,
                ListeningQuestion = Listening.ListeningQuestion,
                ListeningType = Listening.ListeningType,
                DisplayOrder = Listening.DisplayOrder,
                CorrectAnswer = Listening.CorrectAnswer,
                QuestionHtml = Listening.QuestionHtml,
                CreatedAt = Listening.CreatedAt
            };
        }

        public bool Update(int id, UpdateListeningDto dto)
        {
            var Listening = _ListeningRepo.GetById(id);
            if (Listening == null) return false;

            if (dto.ListeningContent != null)
                Listening.ListeningContent = dto.ListeningContent;
            if (dto.ListeningQuestion != null)
                Listening.ListeningQuestion = dto.ListeningQuestion;
            if (dto.ListeningType != null)
                Listening.ListeningType = dto.ListeningType;
            if (dto.DisplayOrder.HasValue)
                Listening.DisplayOrder = dto.DisplayOrder.Value;
            if (dto.CorrectAnswer != null)
                Listening.CorrectAnswer = dto.CorrectAnswer;
            if (dto.QuestionHtml != null)
                Listening.QuestionHtml = dto.QuestionHtml;

            _ListeningRepo.Update(Listening);
            _ListeningRepo.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var Listening = _ListeningRepo.GetById(id);
            if (Listening == null) return false;

            _ListeningRepo.Delete(Listening);
            _ListeningRepo.SaveChanges();
            return true;
        }
    }
}
