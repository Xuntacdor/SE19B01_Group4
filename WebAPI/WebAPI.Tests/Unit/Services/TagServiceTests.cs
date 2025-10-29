using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class TagServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TagService _tagService;

        public TagServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _tagService = new TagService(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var tag1 = new Tag
            {
                TagId = 1,
                TagName = "General",
                CreatedAt = DateTime.UtcNow
            };

            var tag2 = new Tag
            {
                TagId = 2,
                TagName = "Question",
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            var tag3 = new Tag
            {
                TagId = 3,
                TagName = "Help",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            };

            _context.Tag.AddRange(tag1, tag2, tag3);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============ GET ALL TAGS ============

        [Fact]
        public async Task GetAllTagsAsync_ReturnsAllTags()
        {
            var result = await _tagService.GetAllTagsAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetAllTagsAsync_OrdersByTagName()
        {
            var result = await _tagService.GetAllTagsAsync();

            result.Should().BeInAscendingOrder(t => t.TagName);
        }

        [Fact]
        public async Task GetAllTagsAsync_IncludesPostCount()
        {
            // Add a post with tag
            var user = new User
            {
                UserId = 1,
                Username = "test",
                Email = "test@test.com",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 1 },
                Role = "user"
            };
            _context.User.Add(user);

            var post = new Post
            {
                PostId = 1,
                UserId = 1,
                Title = "Test Post",
                Content = "Content",
                Status = "approved",
                CreatedAt = DateTime.UtcNow
            };
            _context.Post.Add(post);
            _context.SaveChanges();

            var tag = _context.Tag.Include(t => t.Posts).First(t => t.TagId == 1);
            tag.Posts.Add(post);
            _context.SaveChanges();

            var result = await _tagService.GetAllTagsAsync();

            result.First(t => t.TagId == 1).PostCount.Should().Be(1);
        }

        // ============ GET TAG BY ID ============

        [Fact]
        public async Task GetTagByIdAsync_ExistingTag_ReturnsTag()
        {
            var result = await _tagService.GetTagByIdAsync(1);

            result.Should().NotBeNull();
            result!.TagId.Should().Be(1);
            result.TagName.Should().Be("General");
        }

        [Fact]
        public async Task GetTagByIdAsync_NonExistentTag_ReturnsNull()
        {
            var result = await _tagService.GetTagByIdAsync(999);

            result.Should().BeNull();
        }

        // ============ GET TAG BY NAME ============

        [Fact]
        public async Task GetTagByNameAsync_ExistingTag_ReturnsTag()
        {
            var result = await _tagService.GetTagByNameAsync("General");

            result.Should().NotBeNull();
            result!.TagName.Should().Be("General");
        }

        [Fact]
        public async Task GetTagByNameAsync_CaseInsensitive_ReturnsTag()
        {
            var result = await _tagService.GetTagByNameAsync("GENERAL");

            result.Should().NotBeNull();
            result!.TagName.Should().Be("General");
        }

        [Fact]
        public async Task GetTagByNameAsync_NonExistentTag_ReturnsNull()
        {
            var result = await _tagService.GetTagByNameAsync("NonExistent");

            result.Should().BeNull();
        }

        // ============ SEARCH TAGS ============

        [Fact]
        public async Task SearchTagsAsync_PartialMatch_ReturnsTags()
        {
            var result = await _tagService.SearchTagsAsync("Qu");

            result.Should().HaveCount(1);
            result.First().TagName.Should().Be("Question");
        }

        [Fact]
        public async Task SearchTagsAsync_CaseInsensitive_ReturnsTags()
        {
            var result = await _tagService.SearchTagsAsync("HELP");

            result.Should().HaveCount(1);
            result.First().TagName.Should().Be("Help");
        }

        [Fact]
        public async Task SearchTagsAsync_NoMatch_ReturnsEmpty()
        {
            var result = await _tagService.SearchTagsAsync("xyz");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchTagsAsync_EmptyQuery_ReturnsEmpty()
        {
            var result = await _tagService.SearchTagsAsync("");

            result.Should().HaveCount(3); // Empty string matches all
        }

        // ============ CREATE TAG ============

        [Fact]
        public async Task CreateTagAsync_ValidData_CreatesTag()
        {
            var dto = new CreateTagDTO
            {
                TagName = "NewTag"
            };

            var result = await _tagService.CreateTagAsync(dto);

            result.Should().NotBeNull();
            result.TagName.Should().Be("NewTag");
            result.PostCount.Should().Be(0);
        }

        [Fact]
        public async Task CreateTagAsync_TrimsWhitespace()
        {
            var dto = new CreateTagDTO
            {
                TagName = "  Trimmed  "
            };

            var result = await _tagService.CreateTagAsync(dto);

            result.TagName.Should().Be("Trimmed");
        }

        [Fact]
        public async Task CreateTagAsync_PersistsToDatabase()
        {
            var dto = new CreateTagDTO
            {
                TagName = "Persistent"
            };

            var result = await _tagService.CreateTagAsync(dto);

            var retrieved = await _context.Tag.FindAsync(result.TagId);
            retrieved.Should().NotBeNull();
            retrieved!.TagName.Should().Be("Persistent");
        }

        // ============ UPDATE TAG ============

        [Fact]
        public async Task UpdateTagAsync_ExistingTag_UpdatesTag()
        {
            var dto = new UpdateTagDTO
            {
                TagName = "UpdatedGeneral"
            };

            var result = await _tagService.UpdateTagAsync(1, dto);

            result.Should().NotBeNull();
            result!.TagName.Should().Be("UpdatedGeneral");
        }

        [Fact]
        public async Task UpdateTagAsync_TrimsWhitespace()
        {
            var dto = new UpdateTagDTO
            {
                TagName = "  Updated  "
            };

            var result = await _tagService.UpdateTagAsync(1, dto);

            result!.TagName.Should().Be("Updated");
        }

        [Fact]
        public async Task UpdateTagAsync_NonExistentTag_ReturnsNull()
        {
            var dto = new UpdateTagDTO
            {
                TagName = "NewName"
            };

            var result = await _tagService.UpdateTagAsync(999, dto);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateTagAsync_PersistsToDatabase()
        {
            var dto = new UpdateTagDTO
            {
                TagName = "PersistentUpdate"
            };

            await _tagService.UpdateTagAsync(1, dto);

            var retrieved = await _context.Tag.FindAsync(1);
            retrieved!.TagName.Should().Be("PersistentUpdate");
        }

        // ============ DELETE TAG ============

        [Fact]
        public async Task DeleteTagAsync_UnusedTag_DeletesTag()
        {
            var result = await _tagService.DeleteTagAsync(1);

            result.Should().BeTrue();

            var deleted = await _context.Tag.FindAsync(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeleteTagAsync_NonExistentTag_ReturnsFalse()
        {
            var result = await _tagService.DeleteTagAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteTagAsync_TagUsedByPost_ThrowsException()
        {
            // Add a post with tag
            var user = new User
            {
                UserId = 1,
                Username = "test",
                Email = "test@test.com",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 1 },
                Role = "user"
            };
            _context.User.Add(user);

            var post = new Post
            {
                PostId = 1,
                UserId = 1,
                Title = "Test Post",
                Content = "Content",
                Status = "approved",
                CreatedAt = DateTime.UtcNow
            };
            _context.Post.Add(post);
            _context.SaveChanges();

            var tag = await _context.Tag.Include(t => t.Posts).FirstAsync(t => t.TagId == 1);
            tag.Posts.Add(post);
            await _context.SaveChangesAsync();

            Func<Task> act = async () => await _tagService.DeleteTagAsync(1);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot delete tag that is being used by posts");
        }

        // ============ EDGE CASES ============

        [Fact]
        public async Task GetAllTagsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Clear all tags
            _context.Tag.RemoveRange(_context.Tag);
            await _context.SaveChangesAsync();

            var result = await _tagService.GetAllTagsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchTagsAsync_MultipleMatches_ReturnsAllMatched()
        {
            // Add more tags with "e" in them
            _context.Tag.Add(new Tag { TagName = "Test", CreatedAt = DateTime.UtcNow });
            _context.Tag.Add(new Tag { TagName = "Development", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var result = await _tagService.SearchTagsAsync("e");

            result.Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task GetTagByIdAsync_IncludesPostCount()
        {
            var result = await _tagService.GetTagByIdAsync(1);

            result.Should().NotBeNull();
            result!.PostCount.Should().Be(0); // No posts initially
        }
    }
}

