using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    internal static class TestUtilities
    {
        public static User CreateValidUser(int id, string username, string email)
        {
            return new User
            {
                UserId = id,
                Username = username,
                Email = email,
                PasswordHash = System.Text.Encoding.UTF8.GetBytes("hashed"),
                PasswordSalt = System.Text.Encoding.UTF8.GetBytes("salt"),
                Role = "User",
                CreatedAt = DateTime.Now
            };
        }

        public static Transaction CreateValidTransaction(int id, int userId, int? planId = null, string status = "PENDING")
        {
            return new Transaction
            {
                TransactionId = id,
                UserId = userId,
                PlanId = planId,
                Amount = 100.0m,
                Currency = "USD",
                Purpose = "Payment",
                Status = status,
                CreatedAt = DateTime.Now
            };
        }

        public static Exam CreateValidExam(int id, string name, string type)
        {
            return new Exam
            {
                ExamId = id,
                ExamName = name,
                ExamType = type
            };
        }

        public static WritingFeedback CreateValidWritingFeedback(long attemptId, int writingId, decimal overall = 7.0m, int? feedbackId = null)
        {
            return new WritingFeedback
            {
                FeedbackId = feedbackId ?? 1,
                AttemptId = attemptId,
                WritingId = writingId,
                TaskAchievement = 7.0m,
                CoherenceCohesion = 7.0m,
                LexicalResource = 7.0m,
                GrammarAccuracy = 7.0m,
                Overall = overall,
                GrammarVocabJson = "{}",
                FeedbackSections = "{}",
                CreatedAt = DateTime.Now
            };
        }

        public static ExamAttempt CreateValidExamAttempt(long attemptId, int examId, int userId)
        {
            return new ExamAttempt
            {
                AttemptId = attemptId,
                ExamId = examId,
                UserId = userId,
                StartedAt = DateTime.Now
            };
        }
    }

    public class AdminRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void CountUsers_WhenUsersExist_ReturnsCount()
        {
            using var context = CreateInMemoryContext();
            context.User.Add(TestUtilities.CreateValidUser(1, "user1", "user1@example.com"));
            context.User.Add(TestUtilities.CreateValidUser(2, "user2", "user2@example.com"));
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.CountUsers();

            Assert.Equal(2, result);
        }

        [Fact]
        public void CountUsers_WhenNoUsers_ReturnsZero()
        {
            using var context = CreateInMemoryContext();
            var repo = new AdminRepository(context);

            var result = repo.CountUsers();

            Assert.Equal(0, result);
        }

        [Fact]
        public void CountExams_WhenExamsExist_ReturnsCount()
        {
            using var context = CreateInMemoryContext();
            context.Exam.Add(TestUtilities.CreateValidExam(1, "Exam1", "TOEIC"));
            context.Exam.Add(TestUtilities.CreateValidExam(2, "Exam2", "TOEFL"));
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.CountExams();

            Assert.Equal(2, result);
        }

        [Fact]
        public void CountExams_WhenNoExams_ReturnsZero()
        {
            using var context = CreateInMemoryContext();
            var repo = new AdminRepository(context);

            var result = repo.CountExams();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetTotalPaidTransactions_WithPaidTransactions_ReturnsTotal()
        {
            using var context = CreateInMemoryContext();
            var t1 = TestUtilities.CreateValidTransaction(1, 1);
            t1.Status = "PAID";
            t1.CreatedAt = DateTime.Now;
            context.Transactions.Add(t1);
            var t2 = TestUtilities.CreateValidTransaction(2, 2);
            t2.Status = "PAID";
            t2.CreatedAt = DateTime.Now;
            context.Transactions.Add(t2);
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.GetTotalPaidTransactions();

            Assert.Equal(200.0m, result);
        }

        [Fact]
        public void GetTotalPaidTransactions_WithNoPaidTransactions_ReturnsZero()
        {
            using var context = CreateInMemoryContext();
            var t1 = TestUtilities.CreateValidTransaction(1, 1);
            t1.Status = "PENDING";
            t1.CreatedAt = DateTime.Now;
            context.Transactions.Add(t1);
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.GetTotalPaidTransactions();

            Assert.Equal(0.0m, result);
        }

        [Fact]
        public void CountExamAttempts_WhenAttemptsExist_ReturnsCount()
        {
            using var context = CreateInMemoryContext();
            context.ExamAttempt.Add(new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1 });
            context.ExamAttempt.Add(new ExamAttempt { AttemptId = 2, ExamId = 1, UserId = 1 });
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.CountExamAttempts();

            Assert.Equal(2, result);
        }

        [Fact]
        public void CountExamAttempts_WhenNoAttempts_ReturnsZero()
        {
            using var context = CreateInMemoryContext();
            var repo = new AdminRepository(context);

            var result = repo.CountExamAttempts();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetMonthlySalesTrend_WithPaidTransactions_ReturnsGroupedData()
        {
            using var context = CreateInMemoryContext();
            var t1 = TestUtilities.CreateValidTransaction(1, 1);
            t1.Status = "PAID";
            t1.CreatedAt = new DateTime(2023, 1, 15);
            context.Transactions.Add(t1);
            var t2 = TestUtilities.CreateValidTransaction(2, 2);
            t2.Status = "PAID";
            t2.CreatedAt = new DateTime(2023, 1, 20);
            context.Transactions.Add(t2);
            var t3 = TestUtilities.CreateValidTransaction(3, 1);
            t3.Status = "PAID";
            t3.CreatedAt = new DateTime(2023, 2, 10);
            context.Transactions.Add(t3);
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.GetMonthlySalesTrend();

            var list = result.ToList();
            Assert.Equal(2, list.Count);

            var jan = list.FirstOrDefault(item => (int)item.GetType().GetProperty("year").GetValue(item) == 2023 && (int)item.GetType().GetProperty("month").GetValue(item) == 1);
            Assert.NotNull(jan);
            Assert.Equal(200.0m, (decimal)jan.GetType().GetProperty("total").GetValue(jan));

            var feb = list.FirstOrDefault(item => (int)item.GetType().GetProperty("year").GetValue(item) == 2023 && (int)item.GetType().GetProperty("month").GetValue(item) == 2);
            Assert.NotNull(feb);
            Assert.Equal(100.0m, (decimal)feb.GetType().GetProperty("total").GetValue(feb));
        }

        [Fact]
        public void GetMonthlySalesTrend_WithNoPaidTransactions_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var t1 = TestUtilities.CreateValidTransaction(1, 1);
            t1.Status = "PENDING";
            t1.CreatedAt = DateTime.Now;
            context.Transactions.Add(t1);
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.GetMonthlySalesTrend();

            Assert.Empty(result);
        }

        [Fact]
        public void GetMonthlySalesTrend_OrderedByYearThenMonth()
        {
            using var context = CreateInMemoryContext();
            var t1 = TestUtilities.CreateValidTransaction(1, 1);
            t1.Status = "PAID";
            t1.CreatedAt = new DateTime(2023, 2, 1);
            context.Transactions.Add(t1);
            var t2 = TestUtilities.CreateValidTransaction(2, 2);
            t2.Status = "PAID";
            t2.CreatedAt = new DateTime(2023, 1, 1);
            context.Transactions.Add(t2);
            var t3 = TestUtilities.CreateValidTransaction(3, 1);
            t3.Status = "PAID";
            t3.CreatedAt = new DateTime(2022, 12, 1);
            context.Transactions.Add(t3);
            context.SaveChanges();

            var repo = new AdminRepository(context);

            var result = repo.GetMonthlySalesTrend();

            var list = result.ToList();
            Assert.Equal(3, list.Count);

            // Should be ordered: 2022-12, 2023-1, 2023-2
            Assert.Equal(2022, (int)list[0].GetType().GetProperty("year").GetValue(list[0]));
            Assert.Equal(12, (int)list[0].GetType().GetProperty("month").GetValue(list[0]));
            Assert.Equal(2023, (int)list[1].GetType().GetProperty("year").GetValue(list[1]));
            Assert.Equal(1, (int)list[1].GetType().GetProperty("month").GetValue(list[1]));
            Assert.Equal(2023, (int)list[2].GetType().GetProperty("year").GetValue(list[2]));
            Assert.Equal(2, (int)list[2].GetType().GetProperty("month").GetValue(list[2]));
        }
    }
}
