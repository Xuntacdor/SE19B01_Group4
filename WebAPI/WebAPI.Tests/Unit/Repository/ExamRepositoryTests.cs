using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class ExamRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsExam()
        {
            using var context = CreateInMemoryContext();
            var exam = new Exam { ExamId = 1, ExamName = "Test Exam", ExamType = "Listening", CreatedAt = DateTime.Now };
            context.Exam.Add(exam);
            context.SaveChanges();

            var repo = new ExamRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal("Test Exam", result.ExamName);
        }

        [Fact]
        public void GetAll_ReturnsAllExams()
        {
            using var context = CreateInMemoryContext();
            context.Exam.Add(new Exam { ExamId = 1, ExamName = "Exam1", ExamType = "Reading" });
            context.Exam.Add(new Exam { ExamId = 2, ExamName = "Exam2", ExamType = "Writing" });
            context.SaveChanges();

            var repo = new ExamRepository(context);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void Add_AddsExam()
        {
            using var context = CreateInMemoryContext();
            var exam = new Exam { ExamId = 1, ExamName = "New Exam", ExamType = "Speaking" };
            var repo = new ExamRepository(context);

            repo.Add(exam);
            repo.SaveChanges();

            Assert.True(context.Exam.Any(e => e.ExamName == "New Exam"));
        }

        [Fact]
        public void Update_UpdatesExam()
        {
            using var context = CreateInMemoryContext();
            var exam = new Exam { ExamId = 1, ExamName = "Old Name", ExamType = "Listening" };
            context.Exam.Add(exam);
            context.SaveChanges();

            exam.ExamName = "New Name";
            var repo = new ExamRepository(context);

            repo.Update(exam);
            repo.SaveChanges();

            var updated = context.Exam.FirstOrDefault(e => e.ExamId == 1);
            Assert.Equal("New Name", updated.ExamName);
        }

        [Fact]
        public void Delete_DeletesExam()
        {
            using var context = CreateInMemoryContext();
            var exam = new Exam { ExamId = 1, ExamName = "To Delete", ExamType = "Writing" };
            context.Exam.Add(exam);
            context.SaveChanges();

            var repo = new ExamRepository(context);

            repo.Delete(exam);
            repo.SaveChanges();

            Assert.False(context.Exam.Any());
        }

        [Fact]
        public void GetAttemptById_WhenExists_ReturnsAttempt()
        {
            using var context = CreateInMemoryContext();
            var exam = new Exam { ExamId = 1, ExamName = "Exam", ExamType = "Test" };
            var user = new User { UserId = 1, Username = "test", Email = "test@test.com", PasswordHash = new byte[0], PasswordSalt = new byte[0], Role = "user", CreatedAt = DateTime.Now };
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            attempt.Exam = exam;
            attempt.User = user;
            context.Exam.Add(exam);
            context.User.Add(user);
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            var repo = new ExamRepository(context);

            var result = repo.GetAttemptById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.AttemptId);
        }

        [Fact]
        public void AddAttempt_AddsExamAttempt()
        {
            using var context = CreateInMemoryContext();
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            var repo = new ExamRepository(context);

            repo.AddAttempt(attempt);
            repo.SaveChanges();

            Assert.True(context.ExamAttempt.Any(a => a.UserId == 1));
        }

        [Fact]
        public void UpdateAttempt_UpdatesExamAttempt()
        {
            using var context = CreateInMemoryContext();
            var user = new User { UserId = 1, Username = "test", Email = "test@test.com", PasswordHash = new byte[0], PasswordSalt = new byte[0], Role = "user", CreatedAt = DateTime.Now };
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now, Score = 0 };
            context.User.Add(user);
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            attempt.Score = 95.5m;
            var repo = new ExamRepository(context);

            repo.UpdateAttempt(attempt);
            repo.SaveChanges();

            var updated = context.ExamAttempt.FirstOrDefault(a => a.AttemptId == 1);
            Assert.Equal(95.5m, updated.Score);
        }

        [Fact]
        public void GetExamAttemptsByUser_ReturnsUserAttempts()
        {
            using var context = CreateInMemoryContext();
            var user1 = new User { UserId = 1, Username = "test1", Email = "test1@test.com", PasswordHash = new byte[0], PasswordSalt = new byte[0], Role = "user", CreatedAt = DateTime.Now };
            var user2 = new User { UserId = 2, Username = "test2", Email = "test2@test.com", PasswordHash = new byte[0], PasswordSalt = new byte[0], Role = "user", CreatedAt = DateTime.Now };
            var exam = new Exam { ExamId = 1, ExamName = "Test Exam", ExamType = "Reading", CreatedAt = DateTime.Now };
            var attempt1 = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now, Score = 100 };
            var attempt2 = new ExamAttempt { AttemptId = 2, ExamId = 1, UserId = 1, StartedAt = DateTime.Now.AddHours(-1), Score = 90 };
            var attempt3 = new ExamAttempt { AttemptId = 3, ExamId = 1, UserId = 2, StartedAt = DateTime.Now, Score = 80 };

            context.User.AddRange(user1, user2);
            context.Exam.Add(exam);
            context.ExamAttempt.AddRange(attempt1, attempt2, attempt3);
            context.SaveChanges();

            var repo = new ExamRepository(context);

            var result = repo.GetExamAttemptsByUser(1);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void GetExamAttemptDetail_WhenExists_ReturnsDetail()
        {
            using var context = CreateInMemoryContext();
            var user = new User { UserId = 1, Username = "test", Email = "test@test.com", PasswordHash = new byte[0], PasswordSalt = new byte[0], Role = "user", CreatedAt = DateTime.Now };
            var exam = new Exam { ExamId = 1, ExamName = "Test Exam", ExamType = "Reading", CreatedAt = DateTime.Now };
            var attempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now, Score = 95, AnswerText = "Answer content" };
            attempt.User = user;
            attempt.Exam = exam;
            context.User.Add(user);
            context.Exam.Add(exam);
            context.ExamAttempt.Add(attempt);
            context.SaveChanges();

            var repo = new ExamRepository(context);

            var result = repo.GetExamAttemptDetail(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.AttemptId);
            Assert.Equal("Test Exam", result.ExamName);
            Assert.Equal(95, result.TotalScore);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new ExamRepository(context);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetAttemptById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new ExamRepository(context);

            var result = repo.GetAttemptById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetExamAttemptDetail_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new ExamRepository(context);

            var result = repo.GetExamAttemptDetail(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetExamAttemptsByUser_WhenNoAttempts_ReturnsEmptyList()
        {
            using var context = CreateInMemoryContext();
            var user = new User { UserId = 1, Username = "test", Email = "test@test.com", PasswordHash = new byte[0], PasswordSalt = new byte[0], Role = "user", CreatedAt = DateTime.Now };
            context.User.Add(user);
            context.SaveChanges();

            var repo = new ExamRepository(context);

            var result = repo.GetExamAttemptsByUser(1);

            Assert.Empty(result);
        }
    }
}
