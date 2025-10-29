using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class ListeningRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsListening()
        {
            using var context = CreateInMemoryContext();
            var listening = new Listening { ListeningId = 1, ExamId = 1, ListeningContent = "Content", ListeningQuestion = "Question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            context.Listening.Add(listening);
            context.SaveChanges();

            var repo = new ListeningRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.ListeningId);
        }

        [Fact]
        public void GetAll_ReturnsAllListenings()
        {
            using var context = CreateInMemoryContext();
            context.Listening.Add(new Listening { ListeningId = 1, ListeningContent = "c1", ListeningQuestion = "q1" });
            context.Listening.Add(new Listening { ListeningId = 2, ListeningContent = "c2", ListeningQuestion = "q2" });
            context.SaveChanges();

            var repo = new ListeningRepository(context);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetByExamId_ReturnsFiltered()
        {
            using var context = CreateInMemoryContext();
            context.Listening.Add(new Listening { ListeningId = 1, ExamId = 1, ListeningContent = "C1", ListeningQuestion = "Q1", DisplayOrder = 1, CreatedAt = DateTime.Now });
            context.Listening.Add(new Listening { ListeningId = 2, ExamId = 2, ListeningContent = "C2", ListeningQuestion = "Q2", DisplayOrder = 1, CreatedAt = DateTime.Now });
            context.SaveChanges();

            var repo = new ListeningRepository(context);

            var result = repo.GetByExamId(1);

            Assert.Single(result);
        }

        [Fact]
        public void Add_AddsListening()
        {
            using var context = CreateInMemoryContext();
            var listening = new Listening { ListeningId = 1, ExamId = 1, ListeningContent = "Add Content", ListeningQuestion = "Add Question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            var repo = new ListeningRepository(context);

            repo.Add(listening);
            repo.SaveChanges();

            Assert.True(context.Listening.Any(l => l.ListeningId == 1));
        }

        [Fact]
        public void Update_UpdatesListening()
        {
            using var context = CreateInMemoryContext();
            var listening = new Listening { ListeningId = 1, ExamId = 1, ListeningContent = "Update Content", ListeningQuestion = "Update Question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            context.Listening.Add(listening);
            context.SaveChanges();

            listening.ExamId = 2;
            var repo = new ListeningRepository(context);

            repo.Update(listening);
            repo.SaveChanges();

            var updated = context.Listening.FirstOrDefault(l => l.ListeningId == 1);
            Assert.Equal(2, updated.ExamId);
        }

        [Fact]
        public void Delete_DeletesListening()
        {
            using var context = CreateInMemoryContext();
            var listening = new Listening { ListeningId = 1, ListeningContent = "Delete Content", ListeningQuestion = "Delete Question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            context.Listening.Add(listening);
            context.SaveChanges();

            var repo = new ListeningRepository(context);

            repo.Delete(listening);
            repo.SaveChanges();

            Assert.False(context.Listening.Any(l => l.ListeningId == 1));
        }
    }
}
