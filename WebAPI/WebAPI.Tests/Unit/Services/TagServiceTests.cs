using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly TagService _service;

        public TagServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new TagService(_context);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var tag1 = new Tag
            {
                TagId = 1,
                TagName = "General",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Posts = new List<Post>()
            };

            var tag2 = new Tag
            {
                TagId = 2,
                TagName = "Question",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Posts = new List<Post>()
            };

            var tag3 = new Tag
            {
                TagId = 3,
                TagName = "Help",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Posts = new List<Post>()
            };

            // Create posts for tag1 to test PostCount
            var post1 = new Post
            {
                PostId = 1,
                UserId = 1,
                Title = "Test Post 1",
                Content = "Content 1",
                Status = "approved",
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tag1 }
            };

            var post2 = new Post
            {
                PostId = 2,
                UserId = 1,
                Title = "Test Post 2",
                Content = "Content 2",
                Status = "approved",
                CreatedAt = DateTime.UtcNow,
                Tags = new List<Tag> { tag1 }
            };

            _context.Tag.AddRange(tag1, tag2, tag3);
            _context.Post.AddRange(post1, post2);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============ GET ALL TAGS ============

        [Fact]
        public void GetAllTags_ReturnsAllTagsOrderedByName()
        {
            var result = _service.GetAllTags();

            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Select(t => t.TagName).Should().BeInAscendingOrder();
            result.First().TagName.Should().Be("General");
        }

        [Fact]
        public void GetAllTags_ReturnsTagsWithPostCount()
        {
            var result = _service.GetAllTags();

            result.Should().NotBeNull();
            var generalTag = result.First(t => t.TagName == "General");
            generalTag.PostCount.Should().Be(2);
            
            var questionTag = result.First(t => t.TagName == "Question");
            questionTag.PostCount.Should().Be(0);
        }

        [Fact]
        public void GetAllTags_ReturnsEmptyList_WhenNoTags()
        {
            // Clear all PostTag relationships first to avoid foreign key constraint issues
            var posts = _context.Post.Include(p => p.Tags).ToList();
            foreach (var post in posts)
            {
                post.Tags.Clear();
            }
            _context.SaveChanges();

            // Now clear all tags
            _context.Tag.RemoveRange(_context.Tag);
            _context.SaveChanges();

            var result = _service.GetAllTags();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // ============ GET TAG BY ID ============

        [Fact]
        public void GetTagById_WhenFound_ReturnsTag()
        {
            var result = _service.GetTagById(1);

            result.Should().NotBeNull();
            result!.TagId.Should().Be(1);
            result.TagName.Should().Be("General");
            result.PostCount.Should().Be(2);
        }

        [Fact]
        public void GetTagById_WhenNotFound_ReturnsNull()
        {
            var result = _service.GetTagById(999);

            result.Should().BeNull();
        }

        [Fact]
        public void GetTagById_ReturnsTagWithCorrectPostCount()
        {
            var result = _service.GetTagById(1);

            result.Should().NotBeNull();
            result!.PostCount.Should().Be(2);
        }

        [Fact]
        public void GetTagById_ReturnsTagWithZeroPostCount_WhenNoPosts()
        {
            var result = _service.GetTagById(2);

            result.Should().NotBeNull();
            result!.PostCount.Should().Be(0);
        }

        // ============ GET TAG BY NAME ============

        [Fact]
        public void GetTagByName_WhenFound_ReturnsTag()
        {
            var result = _service.GetTagByName("General");

            result.Should().NotBeNull();
            result!.TagId.Should().Be(1);
            result.TagName.Should().Be("General");
        }

        [Fact]
        public void GetTagByName_WhenNotFound_ReturnsNull()
        {
            var result = _service.GetTagByName("Nonexistent");

            result.Should().BeNull();
        }

        [Fact]
        public void GetTagByName_IsCaseInsensitive()
        {
            var result1 = _service.GetTagByName("GENERAL");
            var result2 = _service.GetTagByName("general");
            var result3 = _service.GetTagByName("GeNeRaL");

            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result3.Should().NotBeNull();
            result1!.TagId.Should().Be(1);
            result2!.TagId.Should().Be(1);
            result3!.TagId.Should().Be(1);
        }

        [Fact]
        public void GetTagByName_ReturnsTagWithPostCount()
        {
            var result = _service.GetTagByName("General");

            result.Should().NotBeNull();
            result!.PostCount.Should().Be(2);
        }

        // ============ SEARCH TAGS ============

        [Fact]
        public void SearchTags_WhenMatches_ReturnsMatchingTags()
        {
            var result = _service.SearchTags("help");

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().TagName.Should().Be("Help");
        }

        [Fact]
        public void SearchTags_IsCaseInsensitive()
        {
            var result1 = _service.SearchTags("GENERAL");
            var result2 = _service.SearchTags("general");
            var result3 = _service.SearchTags("GeNeRaL");

            result1.Should().HaveCount(1);
            result2.Should().HaveCount(1);
            result3.Should().HaveCount(1);
            result1.First().TagName.Should().Be("General");
        }

        [Fact]
        public void SearchTags_ReturnsPartialMatches()
        {
            var result = _service.SearchTags("ques");

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().TagName.Should().Be("Question");
        }

        [Fact]
        public void SearchTags_ReturnsMultipleMatches()
        {
            // Add another tag with similar name
            var tag4 = new Tag { TagName = "General Knowledge", CreatedAt = DateTime.UtcNow };
            _context.Tag.Add(tag4);
            _context.SaveChanges();

            var result = _service.SearchTags("general");

            result.Should().HaveCount(2);
            result.Should().Contain(t => t.TagName == "General");
            result.Should().Contain(t => t.TagName == "General Knowledge");
        }

        [Fact]
        public void SearchTags_WhenNoMatches_ReturnsEmptyList()
        {
            var result = _service.SearchTags("nonexistent");

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void SearchTags_ReturnsTagsOrderedByName()
        {
            var tag4 = new Tag { TagName = "Alpha", CreatedAt = DateTime.UtcNow };
            var tag5 = new Tag { TagName = "Beta", CreatedAt = DateTime.UtcNow };
            _context.Tag.AddRange(tag4, tag5);
            _context.SaveChanges();

            var result = _service.SearchTags("");

            result.Should().NotBeNull();
            result.Select(t => t.TagName).Should().BeInAscendingOrder();
        }

        // ============ CREATE TAG ============

        [Fact]
        public void CreateTag_ValidData_CreatesTag()
        {
            var createDto = new CreateTagDTO { TagName = "NewTag" };

            var result = _service.CreateTag(createDto);

            result.Should().NotBeNull();
            result.TagId.Should().BeGreaterThan(0);
            result.TagName.Should().Be("NewTag");
            result.PostCount.Should().Be(0);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void CreateTag_TrimsWhitespace()
        {
            var createDto = new CreateTagDTO { TagName = "  TrimmedTag  " };

            var result = _service.CreateTag(createDto);

            result.Should().NotBeNull();
            result.TagName.Should().Be("TrimmedTag");
        }

        [Fact]
        public void CreateTag_SavesToDatabase()
        {
            var createDto = new CreateTagDTO { TagName = "NewTag" };

            _service.CreateTag(createDto);

            var savedTag = _context.Tag.FirstOrDefault(t => t.TagName == "NewTag");
            savedTag.Should().NotBeNull();
            savedTag!.TagName.Should().Be("NewTag");
        }

        [Fact]
        public void CreateTag_SetsCreatedAtToUtcNow()
        {
            var createDto = new CreateTagDTO { TagName = "NewTag" };
            var beforeCreation = DateTime.UtcNow;

            var result = _service.CreateTag(createDto);

            var afterCreation = DateTime.UtcNow;
            result.CreatedAt.Should().BeAfter(beforeCreation.AddSeconds(-1));
            result.CreatedAt.Should().BeBefore(afterCreation.AddSeconds(1));
        }

        // ============ UPDATE TAG ============

        [Fact]
        public void UpdateTag_ValidData_UpdatesTag()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };

            var result = _service.UpdateTag(1, updateDto);

            result.Should().NotBeNull();
            result!.TagId.Should().Be(1);
            result.TagName.Should().Be("UpdatedTag");
            result.PostCount.Should().Be(2); // Should preserve post count
        }

        [Fact]
        public void UpdateTag_TrimsWhitespace()
        {
            var updateDto = new UpdateTagDTO { TagName = "  TrimmedUpdate  " };

            var result = _service.UpdateTag(1, updateDto);

            result.Should().NotBeNull();
            result!.TagName.Should().Be("TrimmedUpdate");
        }

        [Fact]
        public void UpdateTag_UpdatesDatabase()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };

            _service.UpdateTag(1, updateDto);

            var updatedTag = _context.Tag.Find(1);
            updatedTag.Should().NotBeNull();
            updatedTag!.TagName.Should().Be("UpdatedTag");
        }

        [Fact]
        public void UpdateTag_WhenNotFound_ReturnsNull()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };

            var result = _service.UpdateTag(999, updateDto);

            result.Should().BeNull();
        }

        [Fact]
        public void UpdateTag_PreservesCreatedAt()
        {
            var originalTag = _context.Tag.Find(1);
            var originalCreatedAt = originalTag!.CreatedAt;
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };

            var result = _service.UpdateTag(1, updateDto);

            result.Should().NotBeNull();
            result!.CreatedAt.Should().Be(originalCreatedAt);
        }

        [Fact]
        public void UpdateTag_ReturnsCorrectPostCount()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };

            var result = _service.UpdateTag(1, updateDto);

            result.Should().NotBeNull();
            result!.PostCount.Should().Be(2);
        }

        // ============ DELETE TAG ============

        [Fact]
        public void DeleteTag_WhenFound_DeletesTag()
        {
            var result = _service.DeleteTag(2);

            result.Should().BeTrue();
            var deletedTag = _context.Tag.Find(2);
            deletedTag.Should().BeNull();
        }

        [Fact]
        public void DeleteTag_WhenNotFound_ReturnsFalse()
        {
            var result = _service.DeleteTag(999);

            result.Should().BeFalse();
        }

        [Fact]
        public void DeleteTag_WhenTagHasPosts_ThrowsException()
        {
            Action act = () => _service.DeleteTag(1);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot delete tag that is being used by posts");
            
            var tag = _context.Tag.Find(1);
            tag.Should().NotBeNull(); // Tag should still exist
        }

        [Fact]
        public void DeleteTag_WhenTagHasNoPosts_DeletesSuccessfully()
        {
            var result = _service.DeleteTag(2);

            result.Should().BeTrue();
            var deletedTag = _context.Tag.Find(2);
            deletedTag.Should().BeNull();
        }

        [Fact]
        public void DeleteTag_RemovesFromDatabase()
        {
            var tagId = 2;
            var tagExistsBefore = _context.Tag.Any(t => t.TagId == tagId);
            tagExistsBefore.Should().BeTrue();

            _service.DeleteTag(tagId);

            var tagExistsAfter = _context.Tag.Any(t => t.TagId == tagId);
            tagExistsAfter.Should().BeFalse();
        }

        // ============ EDGE CASES ============

        [Fact]
        public void GetAllTags_HandlesTagsWithManyPosts()
        {
            // Add many posts to a tag
            var tag = _context.Tag.Find(2);
            for (int i = 3; i <= 20; i++)
            {
                var post = new Post
                {
                    PostId = i,
                    UserId = 1,
                    Title = $"Post {i}",
                    Content = $"Content {i}",
                    Status = "approved",
                    CreatedAt = DateTime.UtcNow,
                    Tags = new List<Tag> { tag! }
                };
                _context.Post.Add(post);
            }
            _context.SaveChanges();

            var result = _service.GetTagById(2);

            result.Should().NotBeNull();
            result!.PostCount.Should().Be(18);
        }

        [Fact]
        public void SearchTags_HandlesSpecialCharacters()
        {
            var tag = new Tag { TagName = "C# Programming", CreatedAt = DateTime.UtcNow };
            _context.Tag.Add(tag);
            _context.SaveChanges();

            var result = _service.SearchTags("C#");

            result.Should().NotBeNull();
            result.Should().Contain(t => t.TagName == "C# Programming");
        }

        [Fact]
        public void CreateTag_HandlesLongTagNames()
        {
            var longName = new string('a', 50); // Max length based on DTO
            var createDto = new CreateTagDTO { TagName = longName };

            var result = _service.CreateTag(createDto);

            result.Should().NotBeNull();
            result.TagName.Should().Be(longName);
        }

        [Fact]
        public void GetAllTags_ReturnsTagsWithCorrectProperties()
        {
            var result = _service.GetAllTags();

            result.Should().NotBeNull();
            result.Should().OnlyContain(t =>
                t.TagId > 0 &&
                !string.IsNullOrEmpty(t.TagName) &&
                t.CreatedAt != default(DateTime) &&
                t.PostCount >= 0
            );
        }
    }
}

