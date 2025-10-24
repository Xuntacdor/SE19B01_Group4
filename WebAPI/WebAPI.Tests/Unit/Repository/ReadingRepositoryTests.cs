using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests
{
    public class ReadingRepositoryTests
    {
        private Mock<ApplicationDbContext>? _dbContextMock;
        private Mock<DbSet<Reading>>? _dbSetMock;

        private void SetupMockRepository(List<Reading> data)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
            _dbContextMock = new Mock<ApplicationDbContext>(options);

            var queryable = data.AsQueryable();

            _dbSetMock = new Mock<DbSet<Reading>>();
            _dbSetMock.As<IQueryable<Reading>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _dbSetMock.As<IQueryable<Reading>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _dbSetMock.As<IQueryable<Reading>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _dbSetMock.As<IQueryable<Reading>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            _dbSetMock.Setup(m => m.Add(It.IsAny<Reading>())).Callback<Reading>(r => data.Add(r));
            _dbSetMock.Setup(m => m.Update(It.IsAny<Reading>())).Callback<Reading>(r =>
            {
                var idx = data.FindIndex(x => x.ReadingId == r.ReadingId);
                if (idx >= 0)
                    data[idx] = r;
            });
            _dbSetMock.Setup(m => m.Remove(It.IsAny<Reading>())).Callback<Reading>(r =>
            {
                data.RemoveAll(x => x.ReadingId == r.ReadingId);
            });

            _dbContextMock.SetupGet(c => c.Reading).Returns(_dbSetMock.Object);
            _dbContextMock.Setup(c => c.SaveChanges()).Returns(1);
        }

        private Reading CreateSampleReading(int id = 1, int examId = 1, string content = "Sample Content")
        {
            return new Reading
            {
                ReadingId = id,
                ExamId = examId,
                ReadingContent = content,
                ReadingQuestion = "Question",
                ReadingType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = "answer",
                QuestionHtml = "<p>HTML</p>",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public void GetById_WhenExists_ReturnsReading()
        {
            var data = new List<Reading>
            {
                CreateSampleReading(1),
                CreateSampleReading(2)
            };
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);

            var result = repo.GetById(2);

            result.Should().NotBeNull();
            result!.ReadingId.Should().Be(2);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            var data = new List<Reading> { CreateSampleReading(1) };
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);

            var result = repo.GetById(99);

            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_ReturnsAllReadings()
        {
            var data = new List<Reading>
            {
                CreateSampleReading(1),
                CreateSampleReading(2)
            };
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);

            var result = repo.GetAll();

            result.Should().HaveCount(2);
            result.Select(r => r.ReadingId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void GetByExamId_ReturnsFilteredReadings()
        {
            var data = new List<Reading>
            {
                CreateSampleReading(1, examId: 1),
                CreateSampleReading(2, examId: 1),
                CreateSampleReading(3, examId: 2)
            };
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);

            var result = repo.GetByExamId(1);

            result.Should().HaveCount(2);
            result.All(r => r.ExamId == 1).Should().BeTrue();
        }

        [Fact]
        public void Add_AddsReadingAndSavesChanges()
        {
            var data = new List<Reading>();
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);
            var reading = CreateSampleReading(10);

            repo.Add(reading);

            data.Should().ContainSingle(r => r.ReadingId == 10);
            _dbSetMock!.Verify(m => m.Add(It.IsAny<Reading>()), Times.Once);
            _dbContextMock!.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Update_UpdatesReadingAndSavesChanges()
        {
            var data = new List<Reading> { CreateSampleReading(1) };
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);
            var updated = CreateSampleReading(1, content: "Updated Content");

            repo.Update(updated);

            data[0].ReadingContent.Should().Be("Updated Content");
            _dbSetMock!.Verify(m => m.Update(It.IsAny<Reading>()), Times.Once);
            _dbContextMock!.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_RemovesReadingAndSavesChanges()
        {
            var data = new List<Reading> { CreateSampleReading(1), CreateSampleReading(2) };
            SetupMockRepository(data);
            var repo = new ReadingRepository(_dbContextMock!.Object);
            var readingToDelete = data[0];

            repo.Delete(readingToDelete);

            data.Should().HaveCount(1);
            data[0].ReadingId.Should().Be(2);
            _dbSetMock!.Verify(m => m.Remove(It.IsAny<Reading>()), Times.Once);
            _dbContextMock!.Verify(c => c.SaveChanges(), Times.Once);
        }
    }
}
