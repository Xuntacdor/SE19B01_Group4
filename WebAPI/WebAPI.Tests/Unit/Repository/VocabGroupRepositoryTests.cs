using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class VocabGroupRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsVocabGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal("Test Group", result.Groupname);
        }

        [Fact]
        public void GetByName_WhenExists_ReturnsVocabGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "My Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.GetByName(1, "my group");

            Assert.NotNull(result);
            Assert.Equal("My Group", result.Groupname);
        }

        [Fact]
        public void GetByUser_ReturnsUserGroups()
        {
            using var context = CreateInMemoryContext();
            context.VocabGroup.Add(new VocabGroup { GroupId = 1, Groupname = "Group1", UserId = 1, CreatedAt = DateTime.Now });
            context.VocabGroup.Add(new VocabGroup { GroupId = 2, Groupname = "Group2", UserId = 1, CreatedAt = DateTime.Now });
            context.VocabGroup.Add(new VocabGroup { GroupId = 3, Groupname = "Group3", UserId = 2, CreatedAt = DateTime.Now });
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.GetByUser(1);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void ExistsForUser_WhenExists_ReturnsTrue()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Existing", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.ExistsForUser(1, "existing");

            Assert.True(result);
        }

        [Fact]
        public void CountWords_ForGroupWithWords_ReturnsCount()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Count Group", UserId = 1, CreatedAt = DateTime.Now };
            var word1 = new Word { WordId = 1, Term = "Word1" };
            var word2 = new Word { WordId = 2, Term = "Word2" };
            group.Words.Add(word1);
            group.Words.Add(word2);
            context.VocabGroup.Add(group);
            context.Word.Add(word1);
            context.Word.Add(word2);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.CountWords(1);

            Assert.Equal(2, result);
        }

        [Fact]
        public void Add_AddsVocabGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "New Group", UserId = 1, CreatedAt = DateTime.Now };
            var repo = new VocabGroupRepository(context);

            repo.Add(group);
            repo.SaveChanges();

            Assert.True(context.VocabGroup.Any(g => g.Groupname == "New Group"));
        }

        [Fact]
        public void Update_UpdatesVocabGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Old Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            group.Groupname = "Updated Group";
            var repo = new VocabGroupRepository(context);

            repo.Update(group);
            repo.SaveChanges();

            var updated = context.VocabGroup.FirstOrDefault(g => g.GroupId == 1);
            Assert.Equal("Updated Group", updated.Groupname);
        }

        [Fact]
        public void Delete_DeletesVocabGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "To Delete", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            repo.Delete(group);
            repo.SaveChanges();

            Assert.False(context.VocabGroup.Any());
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new VocabGroupRepository(context);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void ExistsForUser_WhenNotExists_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Existing", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.ExistsForUser(1, "nonexisting");

            Assert.False(result);
        }

        [Fact]
        public void CountWords_ForGroupWithNoWords_ReturnsZero()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Empty Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.CountWords(1);

            Assert.Equal(0, result);
        }

        [Fact]
        public void CountWords_GroupNotExists_ReturnsZero()
        {
            using var context = CreateInMemoryContext();
            var repo = new VocabGroupRepository(context);

            var result = repo.CountWords(999);

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetByUser_WhenNoGroups_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Group for User 2", UserId = 2, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.GetByUser(1);

            Assert.Empty(result);
        }

        [Fact]
        public void ExistsForUser_ForDifferentUser_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.ExistsForUser(2, "group");

            Assert.False(result);
        }

        [Fact]
        public void GetByName_CaseInsensitive_ReturnsVocabGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.GetByName(1, "TEST GROUP");

            Assert.NotNull(result);
            Assert.Equal("Test Group", result.Groupname);
        }

        [Fact]
        public void GetByName_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Existing", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var repo = new VocabGroupRepository(context);

            var result = repo.GetByName(1, "nonexisting");

            Assert.Null(result);
        }
    }
}
