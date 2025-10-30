using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Unit.Repository
{
    public class UserSignInHistoryRepositoryTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewInMemoryOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public void Add_ShouldAddEntity()
        {
            var options = CreateNewInMemoryOptions();
            using (var context = new ApplicationDbContext(options))
            {
                var repo = new UserSignInHistoryRepository(context);
                var entity = new UserSignInHistory { SigninId = 99, UserId = 1 };
                repo.Add(entity);
                repo.SaveChanges();
                Assert.Contains(context.UserSignInHistory, h => h.SigninId == 99 && h.UserId == 1);
            }
        }

        [Fact]
        public void SaveChanges_ShouldCallContextSaveChanges()
        {
            var options = CreateNewInMemoryOptions();
            using (var context = new ApplicationDbContext(options))
            {
                var repo = new UserSignInHistoryRepository(context);
                var entity = new UserSignInHistory { SigninId = 123, UserId = 4 };
                repo.Add(entity);
                repo.SaveChanges();
                var saved = context.UserSignInHistory.FirstOrDefault(h => h.SigninId == 123);
                Assert.NotNull(saved);
            }
        }

        [Fact]
        public void GetUserHistory_ShouldReturnFilteredHistory()
        {
            var options = CreateNewInMemoryOptions();
            using (var context = new ApplicationDbContext(options))
            {
                context.UserSignInHistory.AddRange(new List<UserSignInHistory>
                {
                    new UserSignInHistory { SigninId = 1, UserId = 1 },
                    new UserSignInHistory { SigninId = 2, UserId = 2 },
                    new UserSignInHistory { SigninId = 3, UserId = 1 }
                });
                context.SaveChanges();
                var repo = new UserSignInHistoryRepository(context);
                var result = repo.GetUserHistory(1, 10);
                Assert.All(result, h => Assert.Equal(1, h.UserId));
                Assert.Equal(2, result.Count());
            }
        }
    }
}