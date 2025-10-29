using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class UserRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetByEmail_WhenExists_ReturnsUser()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "testuser", "test@example.com");
            context.User.Add(user);
            context.SaveChanges();

            var repo = new UserRepository(context);

            var result = repo.GetByEmail("test@example.com");

            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void GetByEmail_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context);

            var result = repo.GetByEmail("nonexistent@example.com");

            Assert.Null(result);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsUser()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "testuser", "test@example.com");
            context.User.Add(user);
            context.SaveChanges();

            var repo = new UserRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetAll_WhenUsersExist_ReturnsUsers()
        {
            using var context = CreateInMemoryContext();
            context.User.Add(TestUtilities.CreateValidUser(1, "user1", "user1@example.com"));
            context.User.Add(TestUtilities.CreateValidUser(2, "user2", "user2@example.com"));
            context.SaveChanges();

            var repo = new UserRepository(context);

            var result = repo.GetAll().ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.Email == "user1@example.com");
            Assert.Contains(result, u => u.Email == "user2@example.com");
        }

        [Fact]
        public void GetAll_WhenNoUsers_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context);

            var result = repo.GetAll().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void Exists_WhenExists_ReturnsTrue()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "testuser", "test@example.com");
            context.User.Add(user);
            context.SaveChanges();

            var repo = new UserRepository(context);

            var result = repo.Exists(1);

            Assert.True(result);
        }

        [Fact]
        public void Exists_WhenNotExists_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var repo = new UserRepository(context);

            var result = repo.Exists(999);

            Assert.False(result);
        }

        [Fact]
        public void Add_AddsUser()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "newuser", "new@example.com");
            var repo = new UserRepository(context);

            repo.Add(user);
            repo.SaveChanges();

            Assert.True(context.User.Any(u => u.Email == "new@example.com"));
        }

        [Fact]
        public void Update_UpdatesUser()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "testuser", "test@example.com");
            context.User.Add(user);
            context.SaveChanges();

            user.Username = "updateduser";
            var repo = new UserRepository(context);

            repo.Update(user);
            repo.SaveChanges();

            var updated = context.User.FirstOrDefault(u => u.UserId == 1);
            Assert.Equal("updateduser", updated.Username);
        }

        [Fact]
        public void Delete_DeletesUser()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "testuser", "test@example.com");
            context.User.Add(user);
            context.SaveChanges();

            var repo = new UserRepository(context);

            repo.Delete(user);
            repo.SaveChanges();

            Assert.False(context.User.Any());
        }
    }
}
