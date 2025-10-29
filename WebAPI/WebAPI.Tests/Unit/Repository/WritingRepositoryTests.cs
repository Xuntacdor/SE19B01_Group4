using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class WritingRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsWriting()
        {
            using var context = CreateInMemoryContext();
            var writing = new Writing { WritingId = 1, ExamId = 1, WritingQuestion = "Sample question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            context.Writing.Add(writing);
            context.SaveChanges();

            var repo = new WritingRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.WritingId);
        }

        [Fact]
        public void GetByExamId_ReturnsFiltered()
        {
            using var context = CreateInMemoryContext();
            context.Writing.Add(new Writing { WritingId = 1, ExamId = 1, WritingQuestion = "Q1", DisplayOrder = 1, CreatedAt = DateTime.Now });
            context.Writing.Add(new Writing { WritingId = 2, ExamId = 2, WritingQuestion = "Q2", DisplayOrder = 1, CreatedAt = DateTime.Now });
            context.SaveChanges();

            var repo = new WritingRepository(context);

            var result = repo.GetByExamId(1);

            Assert.Single(result);
        }

        [Fact]
        public void Add_AddsWriting()
        {
            using var context = CreateInMemoryContext();
            var writing = new Writing { WritingId = 1, ExamId = 1, WritingQuestion = "Add question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            var repo = new WritingRepository(context);

            repo.Add(writing);
            repo.SaveChanges();

            Assert.True(context.Writing.Any(w => w.WritingId == 1));
        }

        [Fact]
        public void AddExamAttempt_AddsAttempt()
        {
            using var context = CreateInMemoryContext();
            // Assuming ExamAttempt can be added
            var repo = new WritingRepository(context);

            var result = repo.AddExamAttempt(1, 1, "answer");

            Assert.NotNull(result);
            Assert.Equal(1, result.ExamId);
        }

        [Fact]
        public void Update_UpdatesWriting()
        {
            using var context = CreateInMemoryContext();
            var writing = new Writing
            {
                WritingId = 1,
                ExamId = 1,
                WritingQuestion = "Original question",
                DisplayOrder = 1,
                CreatedAt = DateTime.Now
            };
            context.Writing.Add(writing);
            context.SaveChanges();

            var repo = new WritingRepository(context);
            writing.WritingQuestion = "Updated question";
            repo.Update(writing);
            repo.SaveChanges();

            var updated = context.Writing.First(w => w.WritingId == 1);
            Assert.Equal("Updated question", updated.WritingQuestion);
        }

        [Fact]
        public void Delete_DeletesWriting()
        {
            using var context = CreateInMemoryContext();
            var writing = new Writing
            {
                WritingId = 1,
                ExamId = 1,
                WritingQuestion = "Question to delete",
                DisplayOrder = 1,
                CreatedAt = DateTime.Now
            };
            context.Writing.Add(writing);
            context.SaveChanges();

            var repo = new WritingRepository(context);
            repo.Delete(writing);
            repo.SaveChanges();

            Assert.Empty(context.Writing);
        }

        [Fact]
        public void SaveChanges_CommitsChanges()
        {
            using var context = CreateInMemoryContext();
            var repo = new WritingRepository(context);

            // Create a writing
            var writing = new Writing
            {
                ExamId = 1,
                WritingQuestion = "New question",
                DisplayOrder = 1,
                CreatedAt = DateTime.Now
            };
            repo.Add(writing);
            repo.SaveChanges();

            // Verify it was saved
            Assert.Single(context.Writing);

            // Update and save
            writing.WritingQuestion = "Modified question";
            repo.Update(writing);
            repo.SaveChanges();

            // Verify update was saved
            Assert.Equal("Modified question", context.Writing.First().WritingQuestion);

            // Delete and save
            repo.Delete(writing);
            repo.SaveChanges();

            // Verify deletion was saved
            Assert.Empty(context.Writing);
        }

        [Fact]
        public void SaveFeedback_AppendsJsonToAnswerText_WhenAttemptExists()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt
            {
                AttemptId = 123,
                ExamId = 1,
                UserId = 1,
                AnswerText = "User's original answer",
                Score = null,
                StartedAt = DateTime.Now,
                SubmittedAt = DateTime.Now
            };
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            var repo = new WritingRepository(context);

            // Create feedback JSON that would be returned from AI service
            var feedbackJson = JsonDocument.Parse(@"{
                ""task_achievement"": ""Good structure"",
                ""coherence"": ""Well organized"",
                ""lexical_resource"": ""Appropriate vocabulary"",
                ""grammar_accuracy"": 7.5,
                ""band_score"": 7.0
            }");

            repo.SaveFeedback(123, feedbackJson);

            var savedAttempt = context.ExamAttempt.First(a => a.AttemptId == 123);
            Assert.Contains("User's original answer", savedAttempt.AnswerText);
            Assert.Contains("[AI Feedback JSON]", savedAttempt.AnswerText);
            Assert.Contains("Good structure", savedAttempt.AnswerText);
            Assert.Contains("7.5", savedAttempt.AnswerText);
        }

        [Fact]
        public void SaveFeedback_DoesNothing_WhenAttemptNotExists()
        {
            using var context = CreateInMemoryContext();
            var repo = new WritingRepository(context);

            var feedbackJson = JsonDocument.Parse(@"{""test"": ""value""}");

            // Try to save feedback for non-existent attempt ID
            repo.SaveFeedback(999, feedbackJson);

            // Should not throw an exception and should not modify any data
            Assert.Empty(context.ExamAttempt);
        }

        [Fact]
        public void SaveFeedback_HandlesEmptyAnswerText()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt
            {
                AttemptId = 456,
                ExamId = 1,
                UserId = 2,
                AnswerText = "", // Empty answer text
                Score = null,
                StartedAt = DateTime.Now,
                SubmittedAt = DateTime.Now
            };
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            var repo = new WritingRepository(context);
            var feedbackJson = JsonDocument.Parse(@"{""band_score"": 8.0}");

            repo.SaveFeedback(456, feedbackJson);

            var savedAttempt = context.ExamAttempt.First(a => a.AttemptId == 456);
            Assert.StartsWith("\n\n---\n[AI Feedback JSON]\n", savedAttempt.AnswerText);
            Assert.Contains("8.0", savedAttempt.AnswerText);
        }
    }
}
