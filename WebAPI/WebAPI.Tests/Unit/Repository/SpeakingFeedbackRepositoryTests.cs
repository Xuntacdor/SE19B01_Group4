using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Moq;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class SpeakingFeedbackRepositoryTests
    {



        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
            var repo = new SpeakingFeedbackRepository(context, null!);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetByExamAndUser_ReturnsUserFeedbacks()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

            var attempt1 = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            var attempt2 = new ExamAttempt { AttemptId = 2, ExamId = 1, UserId = 2, StartedAt = DateTime.Now };

            var speakingAttempt1 = new SpeakingAttempt { SpeakingAttemptId = 1, SpeakingId = 1, AttemptId = 1 };
            var speakingAttempt2 = new SpeakingAttempt { SpeakingAttemptId = 2, SpeakingId = 2, AttemptId = 2 };

            var feedback1 = new SpeakingFeedback { FeedbackId = 1, SpeakingAttemptId = 1, SpeakingAttempt = speakingAttempt1, Pronunciation = 7.0m, CreatedAt = DateTime.Now };
            var feedback2 = new SpeakingFeedback { FeedbackId = 2, SpeakingAttemptId = 2, SpeakingAttempt = speakingAttempt2, Pronunciation = 8.0m, CreatedAt = DateTime.Now };

            speakingAttempt1.ExamAttempt = attempt1;
            speakingAttempt2.ExamAttempt = attempt2;

            context.ExamAttempt.AddRange(attempt1, attempt2);
            context.SpeakingAttempts.AddRange(speakingAttempt1, speakingAttempt2);
            context.SpeakingFeedbacks.AddRange(feedback1, feedback2);
            context.SaveChanges();

            var repo = new SpeakingFeedbackRepository(context, null!);

            var result = repo.GetByExamAndUser(1, 1);

            Assert.Single(result);
            Assert.Equal(7.0m, result[0].Pronunciation);
        }

        [Fact]
        public void GetBySpeakingAndUser_ReturnsUserFeedback()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

            var examAttempt = new ExamAttempt { AttemptId = 1, ExamId = 1, UserId = 1, StartedAt = DateTime.Now };
            var speakingAttempt = new SpeakingAttempt { SpeakingAttemptId = 1, SpeakingId = 1, AttemptId = 1 };
            var feedback = new SpeakingFeedback { FeedbackId = 1, SpeakingAttemptId = 1, SpeakingAttempt = speakingAttempt, Pronunciation = 7.0m, CreatedAt = DateTime.Now };

            speakingAttempt.ExamAttempt = examAttempt;

            context.ExamAttempt.Add(examAttempt);
            context.SpeakingAttempts.Add(speakingAttempt);
            context.SpeakingFeedbacks.Add(feedback);
            context.SaveChanges();

            var repo = new SpeakingFeedbackRepository(context, null!);

            var result = repo.GetBySpeakingAndUser(1, 1);

            Assert.NotNull(result);
            Assert.Equal(7.0m, result.Pronunciation);
        }

        [Fact]
        public void GetBySpeakingAndUser_WhenNotExists_ReturnsNull()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
            var repo = new SpeakingFeedbackRepository(context, null!);

            var result = repo.GetBySpeakingAndUser(1, 1);

            Assert.Null(result);
        }

        [Fact]
        public void GetByExamAndUser_WhenNoFeedbacks_ReturnsEmptyList()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
            var repo = new SpeakingFeedbackRepository(context, null!);

            var result = repo.GetByExamAndUser(1, 1);

            Assert.Empty(result);
        }

        [Fact]
        public void SaveFeedback_CreatesNewFeedback()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

            // Mock the speaking repo
            var mockSpeakingRepo = new Mock<ISpeakingRepository>();
            var mockAttempt = new SpeakingAttempt { SpeakingAttemptId = 1, SpeakingId = 1, AttemptId = 1 };
            mockSpeakingRepo.Setup(repo => repo.GetOrCreateAttempt(1, 1, 1, "audio.mp3", "transcript")).Returns(mockAttempt);

            var repo = new SpeakingFeedbackRepository(context, mockSpeakingRepo.Object);

            // Create feedback JSON
            var feedbackJson = JsonDocument.Parse(@"{
                ""band_estimate"": {
                    ""pronunciation"": 7.5,
                    ""fluency"": 8.0,
                    ""lexical_resource"": 7.0,
                    ""grammar_accuracy"": 7.5,
                    ""coherence"": 8.0,
                    ""overall"": 7.8
                }
            }");

            repo.SaveFeedback(1, 1, feedbackJson, 1, "audio.mp3", "transcript");

            var feedback = context.SpeakingFeedbacks.FirstOrDefault();
            Assert.NotNull(feedback);
            Assert.Equal(7.5m, feedback.Pronunciation);
            Assert.Equal(8.0m, feedback.Fluency);
            Assert.Equal(7.8m, feedback.Overall);
            Assert.Equal(1, feedback.SpeakingAttemptId);
        }

        [Fact]
        public void SaveFeedback_UpdatesExistingFeedback()
        {
            using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

            // Mock the speaking repo
            var mockSpeakingRepo = new Mock<ISpeakingRepository>();
            var mockAttempt = new SpeakingAttempt { SpeakingAttemptId = 1, SpeakingId = 1, AttemptId = 1 };
            mockSpeakingRepo.Setup(repo => repo.GetOrCreateAttempt(1, 1, 1, "audio.mp3", "transcript")).Returns(mockAttempt);

            var repo = new SpeakingFeedbackRepository(context, mockSpeakingRepo.Object);

            // Pre-existing feedback
            var existingFeedback = new SpeakingFeedback
            {
                FeedbackId = 1,
                SpeakingAttemptId = 1,
                Pronunciation = 6.0m,
                Fluency = 6.5m,
                Overall = 6.2m,
                CreatedAt = DateTime.Now
            };
            context.SpeakingFeedbacks.Add(existingFeedback);
            context.SaveChanges();

            // Create feedback JSON
            var feedbackJson = JsonDocument.Parse(@"{
                ""band_estimate"": {
                    ""pronunciation"": 7.5,
                    ""fluency"": 8.0,
                    ""lexical_resource"": 7.0,
                    ""grammar_accuracy"": 7.5,
                    ""coherence"": 8.0,
                    ""overall"": 7.8
                }
            }");

            repo.SaveFeedback(1, 1, feedbackJson, 1, "audio.mp3", "transcript");

            var feedback = context.SpeakingFeedbacks.First(f => f.FeedbackId == 1);
            Assert.Equal(7.5m, feedback.Pronunciation);
            Assert.Equal(8.0m, feedback.Fluency);
            Assert.Equal(7.8m, feedback.Overall);
            Assert.Equal(1, feedback.SpeakingAttemptId);
        }
    }
}
