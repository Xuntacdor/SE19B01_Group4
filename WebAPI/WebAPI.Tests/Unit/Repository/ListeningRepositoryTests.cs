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
    public class ListeningRepositoryTests
    {
        private Mock<ApplicationDbContext>? _dbContextMock;
        private Mock<DbSet<Listening>>? _dbSetMock;

        private void SetupMockRepository(List<Listening> data)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
            _dbContextMock = new Mock<ApplicationDbContext>(options);

            var queryable = data.AsQueryable();

            _dbSetMock = new Mock<DbSet<Listening>>();
            _dbSetMock.As<IQueryable<Listening>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _dbSetMock.As<IQueryable<Listening>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _dbSetMock.As<IQueryable<Listening>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _dbSetMock.As<IQueryable<Listening>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            _dbSetMock.Setup(m => m.Add(It.IsAny<Listening>())).Callback<Listening>(r => data.Add(r));
            _dbSetMock.Setup(m => m.Update(It.IsAny<Listening>())).Callback<Listening>(r =>
            {
                var idx = data.FindIndex(x => x.ListeningId == r.ListeningId);
                if (idx >= 0)
                    data[idx] = r;
            });
            _dbSetMock.Setup(m => m.Remove(It.IsAny<Listening>())).Callback<Listening>(r =>
            {
                data.RemoveAll(x => x.ListeningId == r.ListeningId);
            });

            _dbContextMock.SetupGet(c => c.Listening).Returns(_dbSetMock.Object);
            _dbContextMock.Setup(c => c.SaveChanges()).Returns(1);
        }

        private Listening CreateSampleListening(int id = 1, int examId = 1, string content = "Sample Content")
        {
            return new Listening
            {
                ListeningId = id,
                ExamId = examId,
                ListeningContent = content,
                ListeningQuestion = "Question",
                ListeningType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = "answer",
                QuestionHtml = "<p>HTML</p>",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public void GetById_WhenExists_ReturnsListening()
        {
            var data = new List<Listening>
            {
                CreateSampleListening(1),
                CreateSampleListening(2)
            };
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);

            var result = repo.GetById(2);

            result.Should().NotBeNull();
            result!.ListeningId.Should().Be(2);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            var data = new List<Listening> { CreateSampleListening(1) };
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);

            var result = repo.GetById(99);

            result.Should().BeNull();
        }

        [Fact]
        public void GetAll_ReturnsAllListenings()
        {
            var data = new List<Listening>
            {
                CreateSampleListening(1),
                CreateSampleListening(2)
            };
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);

            var result = repo.GetAll();

            result.Should().HaveCount(2);
            result.Select(r => r.ListeningId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void GetByExamId_ReturnsFilteredListenings()
        {
            var data = new List<Listening>
            {
                CreateSampleListening(1, examId: 1),
                CreateSampleListening(2, examId: 1),
                CreateSampleListening(3, examId: 2)
            };
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);

            var result = repo.GetByExamId(1);

            result.Should().HaveCount(2);
            result.All(r => r.ExamId == 1).Should().BeTrue();
        }

        [Fact]
        public void Add_AddsListeningAndSavesChanges()
        {
            var data = new List<Listening>();
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);
            var listening = CreateSampleListening(10);

            repo.Add(listening);

            data.Should().ContainSingle(r => r.ListeningId == 10);
            _dbSetMock!.Verify(m => m.Add(It.IsAny<Listening>()), Times.Once);
            _dbContextMock!.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Update_UpdatesListeningAndSavesChanges()
        {
            var data = new List<Listening> { CreateSampleListening(1) };
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);
            var updated = CreateSampleListening(1, content: "Updated Content");

            repo.Update(updated);

            data[0].ListeningContent.Should().Be("Updated Content");
            _dbSetMock!.Verify(m => m.Update(It.IsAny<Listening>()), Times.Once);
            _dbContextMock!.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_RemovesListeningAndSavesChanges()
        {
            var data = new List<Listening> { CreateSampleListening(1), CreateSampleListening(2) };
            SetupMockRepository(data);
            var repo = new ListeningRepository(_dbContextMock!.Object);
            var listeningToDelete = data[0];

            repo.Delete(listeningToDelete);

            data.Should().HaveCount(1);
            data[0].ListeningId.Should().Be(2);
            _dbSetMock!.Verify(m => m.Remove(It.IsAny<Listening>()), Times.Once);
            _dbContextMock!.Verify(c => c.SaveChanges(), Times.Once);
        }
    }
}
