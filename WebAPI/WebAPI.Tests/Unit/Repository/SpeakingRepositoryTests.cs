using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Unit.Repositories
{
    public class SpeakingRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly SpeakingRepository _repo;

        public SpeakingRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique per test
                .Options;

            _context = new ApplicationDbContext(options);
            _repo = new SpeakingRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public void Add_ShouldInsertEntity()
        {
            var entity = new Speaking
            {
                ExamId = 1,
                SpeakingQuestion = "Q1",
                SpeakingType = "Part1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };

            _repo.Add(entity);
            _repo.SaveChanges();

            _context.Speakings.Should().ContainSingle();
        }

        [Fact]
        public void GetById_ShouldReturnEntity_WhenExists()
        {
            var s = new Speaking
            {
                ExamId = 1,
                SpeakingQuestion = "Exists",
                SpeakingType = "Part1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Speakings.Add(s);
            _context.SaveChanges();

            var result = _repo.GetById(s.SpeakingId);

            result.Should().NotBeNull();
            result!.SpeakingQuestion.Should().Be("Exists");
        }

        [Fact]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            _repo.GetById(999).Should().BeNull();
        }

        [Fact]
        public void GetByExamId_ShouldReturnFilteredList()
        {
            _context.Speakings.AddRange(
                new Speaking { ExamId = 10, SpeakingQuestion = "A", SpeakingType = "Part1", DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
                new Speaking { ExamId = 10, SpeakingQuestion = "B", SpeakingType = "Part1", DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
                new Speaking { ExamId = 11, SpeakingQuestion = "C", SpeakingType = "Part2", DisplayOrder = 3, CreatedAt = DateTime.UtcNow }
            );
            _context.SaveChanges();

            var list = _repo.GetByExamId(10).ToList();
            list.Should().HaveCount(2);
        }

        [Fact]
        public void Update_ShouldModifyEntity()
        {
            var s = new Speaking
            {
                ExamId = 1,
                SpeakingQuestion = "Old",
                SpeakingType = "Part1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Speakings.Add(s);
            _context.SaveChanges();

            s.SpeakingQuestion = "Updated";
            _repo.Update(s);
            _repo.SaveChanges();

            _context.Speakings.First().SpeakingQuestion.Should().Be("Updated");
        }

        [Fact]
        public void Delete_ShouldRemoveEntity_WhenExists()
        {
            var s = new Speaking
            {
                ExamId = 1,
                SpeakingQuestion = "ToDelete",
                SpeakingType = "Part1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Speakings.Add(s);
            _context.SaveChanges();

            var result = _repo.Delete(s.SpeakingId);

            result.Should().BeTrue();
            _context.Speakings.Should().BeEmpty();
        }

        [Fact]
        public void Delete_ShouldReturnFalse_WhenNotFound()
        {
            _repo.Delete(999).Should().BeFalse();
        }

        [Fact]
        public void Delete_ShouldRemoveEntityAndCascadeDeleteAttempts_WhenAttemptsExist()
        {
            // Arrange - Create speaking with attempts
            var speaking = new Speaking
            {
                ExamId = 1,
                SpeakingQuestion = "ToDelete",
                SpeakingType = "Part1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Speakings.Add(speaking);
            _context.SaveChanges();

            var examAttempt = new ExamAttempt
            {
                ExamId = 1,
                UserId = 100,
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.ExamAttempt.Add(examAttempt);
            _context.SaveChanges();

            var attempt = new SpeakingAttempt
            {
                AttemptId = examAttempt.AttemptId,
                SpeakingId = speaking.SpeakingId,
                AudioUrl = "test.mp3",
                Transcript = "test transcript",
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.SpeakingAttempts.Add(attempt);
            _context.SaveChanges();

            var feedback = new SpeakingFeedback
            {
                SpeakingAttemptId = attempt.SpeakingAttemptId,
                Pronunciation = 8.5m,
                Fluency = 9.0m,
                AiAnalysisJson = "{}",
                CreatedAt = DateTime.UtcNow
            };
            _context.SpeakingFeedbacks.Add(feedback);
            _context.SaveChanges();

            // Act
            var result = _repo.Delete(speaking.SpeakingId);

            // Assert
            result.Should().BeTrue();
            _context.Speakings.Should().BeEmpty();
            _context.SpeakingAttempts.Should().BeEmpty();
            _context.SpeakingFeedbacks.Should().BeEmpty();
        }

        [Fact]
        public void Delete_ShouldRemoveEntityAndCascadeDeleteAttempts_WhenOnlyAttemptsExist()
        {
            // Arrange - Create speaking with attempts but no feedbacks
            var speaking = new Speaking
            {
                ExamId = 1,
                SpeakingQuestion = "ToDelete",
                SpeakingType = "Part1",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };
            _context.Speakings.Add(speaking);
            _context.SaveChanges();

            var examAttempt = new ExamAttempt
            {
                ExamId = 1,
                UserId = 100,
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.ExamAttempt.Add(examAttempt);
            _context.SaveChanges();

            var attempt = new SpeakingAttempt
            {
                AttemptId = examAttempt.AttemptId,
                SpeakingId = speaking.SpeakingId,
                AudioUrl = "test.mp3",
                Transcript = "test transcript",
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.SpeakingAttempts.Add(attempt);
            _context.SaveChanges();

            // Act
            var result = _repo.Delete(speaking.SpeakingId);

            // Assert
            result.Should().BeTrue();
            _context.Speakings.Should().BeEmpty();
            _context.SpeakingAttempts.Should().BeEmpty();
            _context.SpeakingFeedbacks.Should().BeEmpty();
        }

        [Fact]
        public void GetOrCreateAttempt_ShouldCreateNew_WhenNoExisting()
        {
            var result = _repo.GetOrCreateAttempt(1, 10, 100, "audio.mp3", "text");

            result.Should().NotBeNull();
            _context.SpeakingAttempts.Should().HaveCount(1);
            _context.ExamAttempt.Should().HaveCount(1);
        }

        [Fact]
        public void GetOrCreateAttempt_ShouldReuse_WhenSameAudioExists()
        {
            var examAttempt = new ExamAttempt
            {
                ExamId = 1,
                UserId = 100,
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.ExamAttempt.Add(examAttempt);
            _context.SaveChanges();

            var attempt = new SpeakingAttempt
            {
                AttemptId = examAttempt.AttemptId,
                SpeakingId = 20,
                AudioUrl = "same.mp3",
                Transcript = "text",
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.SpeakingAttempts.Add(attempt);
            _context.SaveChanges();

            var reused = _repo.GetOrCreateAttempt(1, 20, 100, "same.mp3", "text");

            reused.SpeakingAttemptId.Should().Be(attempt.SpeakingAttemptId);
            _context.SpeakingAttempts.Should().HaveCount(1);
        }

        [Fact]
        public void GetOrCreateAttempt_ShouldCreateNew_WhenAudioDifferent()
        {
            var examAttempt = new ExamAttempt
            {
                ExamId = 1,
                UserId = 99,
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            _context.ExamAttempt.Add(examAttempt);
            _context.SpeakingAttempts.Add(new SpeakingAttempt
            {
                AttemptId = examAttempt.AttemptId,
                SpeakingId = 5,
                AudioUrl = "old.mp3",
                Transcript = "abc",
                StartedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = _repo.GetOrCreateAttempt(1, 5, 99, "new.mp3", "updated");

            result.AudioUrl.Should().Be("new.mp3");
            _context.SpeakingAttempts.Count().Should().Be(2);
        }

    }
}
