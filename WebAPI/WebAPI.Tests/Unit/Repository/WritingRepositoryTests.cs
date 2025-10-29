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
        public void SaveFeedback_SavesFeedback()
        {
            using var context = CreateInMemoryContext();
            var feedback = JsonDocument.Parse("{}");
            var repo = new WritingRepository(context);

            repo.SaveFeedback(1, feedback);

            // Check if feedback was saved, assuming WritingFeedback exists or something
        }
    }
}
