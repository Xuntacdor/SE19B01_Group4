using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class ReadingRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsReading()
        {
            using var context = CreateInMemoryContext();
            var reading = new Reading { ReadingId = 1, ExamId = 1, ReadingContent = "content", ReadingQuestion = "question" };
            context.Reading.Add(reading);
            context.SaveChanges();

            var repo = new ReadingRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.ReadingId);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new ReadingRepository(context);

            var result = repo.GetById(1);

            Assert.Null(result);
        }

        [Fact]
        public void GetAll_ReturnsAllReadings()
        {
            using var context = CreateInMemoryContext();
            context.Reading.Add(new Reading { ReadingId = 1, ReadingContent = "c1", ReadingQuestion = "q1" });
            context.Reading.Add(new Reading { ReadingId = 2, ReadingContent = "c2", ReadingQuestion = "q2" });
            context.SaveChanges();

            var repo = new ReadingRepository(context);

            var result = repo.GetAll();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetByExamId_ReturnsFiltered()
        {
            using var context = CreateInMemoryContext();
            context.Reading.Add(new Reading { ReadingId = 1, ExamId = 1, ReadingContent = "c1", ReadingQuestion = "q1" });
            context.Reading.Add(new Reading { ReadingId = 2, ExamId = 2, ReadingContent = "c2", ReadingQuestion = "q2" });
            context.SaveChanges();

            var repo = new ReadingRepository(context);

            var result = repo.GetByExamId(1);

            Assert.Single(result);
        }

        [Fact]
        public void Add_AddsReading()
        {
            using var context = CreateInMemoryContext();
            var reading = new Reading { ReadingId = 1, ExamId = 1, ReadingContent = "content", ReadingQuestion = "question" };
            var repo = new ReadingRepository(context);

            repo.Add(reading);
            repo.SaveChanges();

            var added = context.Reading.FirstOrDefault(r => r.ReadingId == 1);
            Assert.NotNull(added);
        }

        [Fact]
        public void Update_UpdatesReading()
        {
            using var context = CreateInMemoryContext();
            var reading = new Reading { ReadingId = 1, ExamId = 1, ReadingContent = "Update Content", ReadingQuestion = "Update Question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            context.Reading.Add(reading);
            context.SaveChanges();

            reading.ExamId = 2;
            var repo = new ReadingRepository(context);

            repo.Update(reading);
            repo.SaveChanges();

            var updated = context.Reading.FirstOrDefault(r => r.ReadingId == 1);
            Assert.Equal(2, updated.ExamId);
        }

        [Fact]
        public void Delete_DeletesReading()
        {
            using var context = CreateInMemoryContext();
            var reading = new Reading { ReadingId = 1, ReadingContent = "Delete Content", ReadingQuestion = "Delete Question", DisplayOrder = 1, CreatedAt = DateTime.Now };
            context.Reading.Add(reading);
            context.SaveChanges();

            var repo = new ReadingRepository(context);

            repo.Delete(reading);
            repo.SaveChanges();

            var deleted = context.Reading.FirstOrDefault(r => r.ReadingId == 1);
            Assert.Null(deleted);
        }
    }
}
