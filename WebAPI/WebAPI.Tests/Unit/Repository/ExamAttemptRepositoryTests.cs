using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class ExamAttemptRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsAttempt()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            var repo = new ExamAttemptRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.ExamId);
        }

        [Fact]
        public void GetByExamId_ReturnsAttemptsForExam()
        {
            using var context = CreateInMemoryContext();
            context.ExamAttempt.Add(new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now });
            context.ExamAttempt.Add(new ExamAttempt { AttemptId = 2, ExamId = 1, UserId = 2, StartedAt = DateTime.Now });
            context.ExamAttempt.Add(new ExamAttempt { AttemptId = 3, ExamId = 2, UserId = 1, StartedAt = DateTime.Now });
            context.SaveChanges();

            var repo = new ExamAttemptRepository(context);

            var result = repo.GetByExamId(1);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Add_AddsExamAttempt()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            var repo = new ExamAttemptRepository(context);

            repo.Add(attempt);
            repo.SaveChanges();

            Assert.True(context.ExamAttempt.Any(a => a.UserId == 1));
        }

        [Fact]
        public void Update_UpdatesExamAttempt()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            attempt.ExamId = 2;
            var repo = new ExamAttemptRepository(context);

            repo.Update(attempt);
            repo.SaveChanges();

            var updated = context.ExamAttempt.FirstOrDefault(a => a.AttemptId == 1);
            Assert.Equal(2, updated.ExamId);
        }

        [Fact]
        public void Delete_DeletesExamAttempt()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            var repo = new ExamAttemptRepository(context);

            repo.Delete(attempt);
            repo.SaveChanges();

            Assert.False(context.ExamAttempt.Any());
        }
    }
}
