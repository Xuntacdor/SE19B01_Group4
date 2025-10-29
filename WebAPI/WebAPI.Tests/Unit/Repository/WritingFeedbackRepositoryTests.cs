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
    public class WritingFeedbackRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void WritingFeedbackRepository_GetAll_WhenFeedbackExists_ReturnsAll()
        {
            using var context = CreateInMemoryContext();
            var attempt1 = TestUtilities.CreateValidExamAttempt(1, 1, 1);
            var attempt2 = TestUtilities.CreateValidExamAttempt(2, 1, 1);
            context.ExamAttempt.Add(attempt1);
            context.ExamAttempt.Add(attempt2);

            var feedback1 = TestUtilities.CreateValidWritingFeedback(1, 1, 7.0m, 1);
            var feedback2 = TestUtilities.CreateValidWritingFeedback(2, 2, 8.0m, 2);
            context.WritingFeedback.Add(feedback1);
            context.WritingFeedback.Add(feedback2);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.AttemptId == 1 && f.WritingId == 1);
            Assert.Contains(result, f => f.AttemptId == 2 && f.WritingId == 2);
        }

        [Fact]
        public void GetAll_WhenNoFeedback_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetAll();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsFeedback()
        {
            using var context = CreateInMemoryContext();
            var attempt = TestUtilities.CreateValidExamAttempt(1, 1, 1);
            context.ExamAttempt.Add(attempt);

            var feedback = TestUtilities.CreateValidWritingFeedback(1, 1, 8.5m, 1);
            context.WritingFeedback.Add(feedback);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.FeedbackId);
            Assert.Equal(8.5m, result.Overall);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetByExamAndUser_WhenExists_ReturnsFeedbackOrderedByCreatedAtDesc()
        {
            using var context = CreateInMemoryContext();
            var user1 = TestUtilities.CreateValidUser(1, "user1", "user1@example.com");
            var user2 = TestUtilities.CreateValidUser(2, "user2", "user2@example.com");
            var exam1 = TestUtilities.CreateValidExam(1, "Exam1", "TOEIC");
            var exam2 = TestUtilities.CreateValidExam(2, "Exam2", "TOEFL");

            var attempt1 = TestUtilities.CreateValidExamAttempt(1, 1, 1); // exam1, user1
            var attempt2 = TestUtilities.CreateValidExamAttempt(2, 1, 2); // exam1, user2
            var attempt3 = TestUtilities.CreateValidExamAttempt(3, 2, 1); // exam2, user1

            context.User.Add(user1);
            context.User.Add(user2);
            context.Exam.Add(exam1);
            context.Exam.Add(exam2);
            context.ExamAttempt.Add(attempt1);
            context.ExamAttempt.Add(attempt2);
            context.ExamAttempt.Add(attempt3);

            var fb1 = TestUtilities.CreateValidWritingFeedback(1, 1, 7.0m, 1);
            fb1.CreatedAt = new DateTime(2023, 1, 1);
            var fb2 = TestUtilities.CreateValidWritingFeedback(1, 1, 7.0m, 2);
            fb2.CreatedAt = new DateTime(2023, 1, 2);
            var fb3 = TestUtilities.CreateValidWritingFeedback(3, 2, 7.0m, 3); // different attempt

            context.WritingFeedback.Add(fb1);
            context.WritingFeedback.Add(fb2);
            context.WritingFeedback.Add(fb3);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByExamAndUser(1, 1); // exam1, user1

            Assert.Equal(2, result.Count); // fb2 then fb1 (descending time)
            Assert.Equal(2, result[0].FeedbackId); // latest first
            Assert.Equal(1, result[1].FeedbackId);
        }

        [Fact]
        public void GetByExamAndUser_WhenNoMatchingFeedback_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "user", "user@example.com");
            var exam = TestUtilities.CreateValidExam(1, "Exam", "TOEIC");
            var attempt = TestUtilities.CreateValidExamAttempt(1, 1, 1);

            context.User.Add(user);
            context.Exam.Add(exam);
            context.ExamAttempt.Add(attempt);

            var fb = TestUtilities.CreateValidWritingFeedback(1, 1);
            context.WritingFeedback.Add(fb);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByExamAndUser(2, 1); // non-existing exam

            Assert.Empty(result);
        }

        [Fact]
        public void GetByExamAndUser_WhenExamAttemptIsNull_ExcludesIt()
        {
            using var context = CreateInMemoryContext();
            // Add feedback with attempt id that doesn't exist
            var fb = TestUtilities.CreateValidWritingFeedback(1, 1, 1); // attemptId=1, but no such attempt
            context.WritingFeedback.Add(fb);
            context.SaveChanges(); // This should succeed but GetByExamAndUser won't include it

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByExamAndUser(1, 1); // even with non-existing attempt

            Assert.Empty(result);
        }

        [Fact]
        public void GetByAttemptAndWriting_WhenExists_ReturnsFeedback()
        {
            using var context = CreateInMemoryContext();
            var fb1 = TestUtilities.CreateValidWritingFeedback(1, 1, 7.0m, 1);
            var fb2 = TestUtilities.CreateValidWritingFeedback(1, 2, 8.0m, 2); // different writing
            context.WritingFeedback.Add(fb1);
            context.WritingFeedback.Add(fb2);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByAttemptAndWriting(1, 1);

            Assert.NotNull(result);
            Assert.Equal(1, result.FeedbackId);
            Assert.Equal(7.0m, result.Overall);
        }

        [Fact]
        public void GetByAttemptAndWriting_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByAttemptAndWriting(1, 1);

            Assert.Null(result);
        }

        [Fact]
        public void GetByAttemptAndWriting_WhenWrongWriting_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var fb = TestUtilities.CreateValidWritingFeedback(1, 1, 7.0m);
            context.WritingFeedback.Add(fb);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByAttemptAndWriting(1, 2); // wrong writing

            Assert.Null(result);
        }

        [Fact]
        public void GetByAttemptAndWriting_WhenWrongAttempt_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var fb = TestUtilities.CreateValidWritingFeedback(1, 1, 7.0m);
            context.WritingFeedback.Add(fb);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            var result = repo.GetByAttemptAndWriting(2, 1); // wrong attempt

            Assert.Null(result);
        }

        [Fact]
        public void Add_AddsFeedback()
        {
            using var context = CreateInMemoryContext();
            var feedback = TestUtilities.CreateValidWritingFeedback(1, 1); // don't set id since it's key
            var repo = new WritingFeedbackRepository(context);

            repo.Add(feedback);
            repo.SaveChanges();

            Assert.True(context.WritingFeedback.Any(f => f.AttemptId == 1 && f.WritingId == 1));
        }

        [Fact]
        public void Update_UpdatesFeedback()
        {
            using var context = CreateInMemoryContext();
            var feedback = TestUtilities.CreateValidWritingFeedback(1, 1, 6.0m);
            context.WritingFeedback.Add(feedback);
            context.SaveChanges();

            feedback.Overall = 9.0m;
            var repo = new WritingFeedbackRepository(context);

            repo.Update(feedback);
            repo.SaveChanges();

            var updated = context.WritingFeedback.FirstOrDefault(f => f.FeedbackId == 1);
            Assert.Equal(9.0m, updated?.Overall);
        }

        [Fact]
        public void Delete_DeletesFeedback()
        {
            using var context = CreateInMemoryContext();
            var feedback = TestUtilities.CreateValidWritingFeedback(1, 1);
            context.WritingFeedback.Add(feedback);
            context.SaveChanges();

            var repo = new WritingFeedbackRepository(context);

            repo.Delete(feedback);
            repo.SaveChanges();

            Assert.False(context.WritingFeedback.Any());
        }
    }
}
