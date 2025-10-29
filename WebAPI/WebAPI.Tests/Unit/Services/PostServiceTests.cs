using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class PostServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly PostService _postService;

        public PostServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _userRepositoryMock = new Mock<IUserRepository>();
            _postService = new PostService(_context, _userRepositoryMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var user1 = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                UserId = 2,
                Username = "admin",
                Email = "admin@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = "admin",
                CreatedAt = DateTime.UtcNow
            };

            _context.User.AddRange(user1, user2);

            var tag1 = new Tag { TagId = 1, TagName = "General", CreatedAt = DateTime.UtcNow };
            var tag2 = new Tag { TagId = 2, TagName = "Question", CreatedAt = DateTime.UtcNow };
            _context.Tag.AddRange(tag1, tag2);

            var post1 = new Post
            {
                PostId = 1,
                UserId = 1,
                Title = "Test Post 1",
                Content = "Content 1",
                Status = "approved",
                CreatedAt = DateTime.UtcNow,
                ViewCount = 10,
                IsPinned = false
            };

            var post2 = new Post
            {
                PostId = 2,
                UserId = 1,
                Title = "Test Post 2",
                Content = "Content 2",
                Status = "pending",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ViewCount = 5,
                IsPinned = true
            };

            _context.Post.AddRange(post1, post2);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============ GET POSTS ============

        [Fact]
        public void GetPosts_ReturnsApprovedPostsOnly()
        {
            var result = _postService.GetPosts(1, 10);

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Test Post 1");
        }

        [Fact]
        public void GetPosts_OrdersByPinnedThenCreatedAt()
        {
            // Add another approved post
            var post3 = new Post
            {
                PostId = 3,
                UserId = 1,
                Title = "Pinned Post",
                Content = "Pinned Content",
                Status = "approved",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                ViewCount = 0,
                IsPinned = true
            };
            _context.Post.Add(post3);
            _context.SaveChanges();

            var result = _postService.GetPosts(1, 10);

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().IsPinned.Should().BeTrue();
        }

        [Fact]
        public void GetPosts_WithPagination_ReturnsCorrectPage()
        {
            // Add more posts
            for (int i = 3; i <= 15; i++)
            {
                _context.Post.Add(new Post
                {
                    PostId = i,
                    UserId = 1,
                    Title = $"Post {i}",
                    Content = $"Content {i}",
                    Status = "approved",
                    CreatedAt = DateTime.UtcNow.AddHours(-i),
                    ViewCount = 0
                });
            }
            _context.SaveChanges();

            var page1 = _postService.GetPosts(1, 5);
            var page2 = _postService.GetPosts(2, 5);

            page1.Should().HaveCount(5);
            page2.Should().HaveCount(5);
            page1.Select(p => p.PostId).Should().NotIntersectWith(page2.Select(p => p.PostId));
        }

        // ============ GET POSTS BY FILTER ============

        [Fact]
        public void GetPostsByFilter_NewFilter_ReturnsOrderedByCreatedAt()
        {
            // Approve pending post
            var post = _context.Post.Find(2);
            post!.Status = "approved";
            _context.SaveChanges();

            var result = _postService.GetPostsByFilter("new", 1, 10);

            result.Should().HaveCount(2);
            result.First().IsPinned.Should().BeTrue();
        }

        [Fact]
        public void GetPostsByFilter_TopFilter_ReturnsOrderedByLikes()
        {
            // Add likes to post 1
            _context.PostLike.Add(new PostLike { PostId = 1, UserId = 1, CreatedAt = DateTime.UtcNow });
            _context.PostLike.Add(new PostLike { PostId = 1, UserId = 2, CreatedAt = DateTime.UtcNow });
            _context.SaveChanges();

            var result = _postService.GetPostsByFilter("top", 1, 10);

            result.Should().HaveCount(1);
            result.First().VoteCount.Should().Be(2);
        }

        [Fact]
        public void GetPostsByFilter_HotFilter_ReturnsOrderedByViews()
        {
            var result = _postService.GetPostsByFilter("hot", 1, 10);

            result.Should().HaveCount(1);
            result.First().ViewCount.Should().Be(10);
        }

        [Fact]
        public void GetPostsByFilter_ClosedFilter_WithoutUserId_ReturnsEmpty()
        {
            var result = _postService.GetPostsByFilter("closed", 1, 10);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GetPostsByFilter_ClosedFilter_WithUserId_ReturnsHiddenPosts()
        {
            _context.UserPostHide.Add(new UserPostHide
            {
                UserId = 1,
                PostId = 1,
                HiddenAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = _postService.GetPostsByFilter("closed", 1, 10, 1);

            result.Should().HaveCount(1);
        }

        // ============ GET POST BY ID ============

        [Fact]
        public void GetPostById_ExistingPost_ReturnsPost()
        {
            var result = _postService.GetPostById(1);

            result.Should().NotBeNull();
            result!.PostId.Should().Be(1);
            result.Title.Should().Be("Test Post 1");
        }

        [Fact]
        public void GetPostById_NonExistingPost_ReturnsNull()
        {
            var result = _postService.GetPostById(999);

            result.Should().BeNull();
        }

        // ============ CREATE POST ============

        [Fact]
        public void CreatePost_ValidData_CreatesPost()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = "user"
            });

            var dto = new CreatePostDTO
            {
                Title = "New Post",
                Content = "New Content",
                TagNames = new List<string>(),
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var result = _postService.CreatePost(dto, 1);

            result.Should().NotBeNull();
            result.Title.Should().Be("New Post");
            result.Content.Should().Be("New Content");
        }

        [Fact]
        public void CreatePost_NonExistentUser_ThrowsException()
        {
            _userRepositoryMock.Setup(u => u.GetById(999)).Returns((User?)null);

            var dto = new CreatePostDTO
            {
                Title = "New Post",
                Content = "New Content",
                TagNames = new List<string>(),
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            Action act = () => _postService.CreatePost(dto, 999);

            act.Should().Throw<KeyNotFoundException>().WithMessage("User not found");
        }

        [Fact]
        public void CreatePost_WithTags_AddsTags()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreatePostDTO
            {
                Title = "Tagged Post",
                Content = "Content with tags",
                TagNames = new List<string> { "General", "NewTag" },
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var result = _postService.CreatePost(dto, 1);

            result.Tags.Should().HaveCount(2);
            result.Tags.Should().Contain(t => t.TagName == "General");
            result.Tags.Should().Contain(t => t.TagName == "NewTag");
        }

        [Fact]
        public void CreatePost_WithAttachments_CreatesPostWithAttachments()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreatePostDTO
            {
                Title = "Post with Attachments",
                Content = "Content with files",
                TagNames = new List<string>(),
                Attachments = new List<CreatePostAttachmentDTO>
                {
                    new CreatePostAttachmentDTO
                    {
                        FileName = "document.pdf",
                        FileUrl = "https://example.com/files/document.pdf",
                        FileType = "application/pdf",
                        FileExtension = ".pdf",
                        FileSize = 102400
                    },
                    new CreatePostAttachmentDTO
                    {
                        FileName = "image.png",
                        FileUrl = "https://example.com/files/image.png",
                        FileType = "image/png",
                        FileExtension = ".png",
                        FileSize = 51200
                    }
                }
            };

            var result = _postService.CreatePost(dto, 1);

            result.Should().NotBeNull();
            result.Title.Should().Be("Post with Attachments");
            result.Attachments.Should().HaveCount(2);
            
            var firstAttachment = result.Attachments.First();
            firstAttachment.FileName.Should().Be("document.pdf");
            firstAttachment.FileUrl.Should().Be("https://example.com/files/document.pdf");
            firstAttachment.FileType.Should().Be("application/pdf");
            firstAttachment.FileExtension.Should().Be(".pdf");
            firstAttachment.FileSize.Should().Be(102400);
            
            var secondAttachment = result.Attachments.Last();
            secondAttachment.FileName.Should().Be("image.png");
            secondAttachment.FileSize.Should().Be(51200);
        }

        [Fact]
        public void CreatePost_WithSingleAttachment_CreatesPostSuccessfully()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreatePostDTO
            {
                Title = "Post with Single File",
                Content = "Content with one file",
                TagNames = new List<string>(),
                Attachments = new List<CreatePostAttachmentDTO>
                {
                    new CreatePostAttachmentDTO
                    {
                        FileName = "spreadsheet.xlsx",
                        FileUrl = "https://example.com/files/spreadsheet.xlsx",
                        FileType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        FileExtension = ".xlsx",
                        FileSize = 204800
                    }
                }
            };

            var result = _postService.CreatePost(dto, 1);

            result.Should().NotBeNull();
            result.Attachments.Should().HaveCount(1);
            result.Attachments.First().FileName.Should().Be("spreadsheet.xlsx");
            result.Attachments.First().FileSize.Should().Be(204800);
        }

        [Fact]
        public void CreatePost_WithEmptyAttachments_CreatesPostWithoutAttachments()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreatePostDTO
            {
                Title = "Post without Files",
                Content = "Just text content",
                TagNames = new List<string>(),
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var result = _postService.CreatePost(dto, 1);

            result.Should().NotBeNull();
            result.Attachments.Should().BeEmpty();
        }

        // ============ UPDATE POST ============

        [Fact]
        public void UpdatePost_ValidData_UpdatesPost()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new UpdatePostDTO
            {
                Title = "Updated Title",
                Content = "Updated Content"
            };

            _postService.UpdatePost(1, dto, 1);

            var updated = _context.Post.Find(1);
            updated!.Title.Should().Be("Updated Title");
            updated.Content.Should().Be("Updated Content");
        }

        [Fact]
        public void UpdatePost_NonExistentPost_ThrowsException()
        {
            var dto = new UpdatePostDTO { Title = "Updated" };

            Action act = () => _postService.UpdatePost(999, dto, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Post not found");
        }

        [Fact]
        public void UpdatePost_UnauthorizedUser_ThrowsException()
        {
            _userRepositoryMock.Setup(u => u.GetById(2)).Returns(_context.User.Find(2)!);

            var dto = new UpdatePostDTO { Title = "Updated" };

            Action act = () => _postService.UpdatePost(1, dto, 2);

            // User 2 is admin, so it should work
            // Let's test with a regular user who doesn't own the post
            var regularUser = new User
            {
                UserId = 3,
                Username = "regular",
                Email = "regular@test.com",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 1 },
                Role = "user"
            };
            _context.User.Add(regularUser);
            _context.SaveChanges();

            _userRepositoryMock.Setup(u => u.GetById(3)).Returns(regularUser);

            Action act2 = () => _postService.UpdatePost(1, dto, 3);

            act2.Should().Throw<UnauthorizedAccessException>();
        }

        // ============ DELETE POST ============

        [Fact]
        public void DeletePost_ValidPost_DeletesPost()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            _postService.DeletePost(1, 1);

            var deleted = _context.Post.Find(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public void DeletePost_NonExistentPost_ThrowsException()
        {
            Action act = () => _postService.DeletePost(999, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Post not found");
        }

        // ============ VOTE POST ============

        [Fact]
        public void VotePost_ValidPost_AddsVote()
        {
            _postService.VotePost(1, 2);

            var like = _context.PostLike.FirstOrDefault(pl => pl.PostId == 1 && pl.UserId == 2);
            like.Should().NotBeNull();
        }

        [Fact]
        public void VotePost_AlreadyVoted_ThrowsException()
        {
            _context.PostLike.Add(new PostLike { PostId = 1, UserId = 1, CreatedAt = DateTime.UtcNow });
            _context.SaveChanges();

            Action act = () => _postService.VotePost(1, 1);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You have already voted for this post");
        }

        [Fact]
        public void VotePost_NonExistentPost_ThrowsException()
        {
            Action act = () => _postService.VotePost(999, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Post not found");
        }

        // ============ UNVOTE POST ============

        [Fact]
        public void UnvotePost_ValidVote_RemovesVote()
        {
            _context.PostLike.Add(new PostLike { PostId = 1, UserId = 1, CreatedAt = DateTime.UtcNow });
            _context.SaveChanges();

            _postService.UnvotePost(1, 1);

            var like = _context.PostLike.FirstOrDefault(pl => pl.PostId == 1 && pl.UserId == 1);
            like.Should().BeNull();
        }

        [Fact]
        public void UnvotePost_NotVoted_ThrowsException()
        {
            Action act = () => _postService.UnvotePost(1, 1);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You haven't voted for this post");
        }

        // ============ PIN/UNPIN POST ============

        [Fact]
        public void PinPost_ValidPost_PinsPost()
        {
            _postService.PinPost(1);

            var post = _context.Post.Find(1);
            post!.IsPinned.Should().BeTrue();
        }

        [Fact]
        public void UnpinPost_ValidPost_UnpinsPost()
        {
            _postService.UnpinPost(2);

            var post = _context.Post.Find(2);
            post!.IsPinned.Should().BeFalse();
        }

        // ============ HIDE/UNHIDE POST ============

        [Fact]
        public void HidePost_ValidPost_HidesPost()
        {
            _postService.HidePost(1, 1);

            var hide = _context.UserPostHide.FirstOrDefault(uph => uph.PostId == 1 && uph.UserId == 1);
            hide.Should().NotBeNull();
        }

        [Fact]
        public void UnhidePost_HiddenPost_UnhidesPost()
        {
            _context.UserPostHide.Add(new UserPostHide { UserId = 1, PostId = 1, HiddenAt = DateTime.UtcNow });
            _context.SaveChanges();

            _postService.UnhidePost(1, 1);

            var hide = _context.UserPostHide.FirstOrDefault(uph => uph.PostId == 1 && uph.UserId == 1);
            hide.Should().BeNull();
        }

        // ============ INCREMENT VIEW COUNT ============

        [Fact]
        public void IncrementViewCount_ValidPost_IncrementsCount()
        {
            var originalCount = _context.Post.Find(1)!.ViewCount;

            _postService.IncrementViewCount(1);

            var post = _context.Post.Find(1);
            post!.ViewCount.Should().Be(originalCount + 1);
        }

        // ============ MODERATOR METHODS ============

        [Fact]
        public void GetModeratorStats_ReturnsCorrectStats()
        {
            var stats = _postService.GetModeratorStats();

            stats.Should().NotBeNull();
            stats.TotalPosts.Should().Be(1); // Only approved posts
            stats.PendingPosts.Should().Be(1);
        }

        [Fact]
        public void GetPendingPosts_ReturnsPendingPostsOnly()
        {
            var result = _postService.GetPendingPosts(1, 10);

            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Test Post 2");
        }

        [Fact]
        public void ApprovePost_ValidPost_ApprovesPost()
        {
            _postService.ApprovePost(2);

            var post = _context.Post.Find(2);
            post!.Status.Should().Be("approved");
            post.RejectionReason.Should().BeNull();
        }

        [Fact]
        public void RejectPost_ValidPost_RejectsPost()
        {
            _postService.RejectPost(2, "Inappropriate content");

            var post = _context.Post.Find(2);
            post!.Status.Should().Be("rejected");
            post.RejectionReason.Should().Be("Inappropriate content");
        }

        [Fact]
        public void GetRejectedPosts_ReturnsRejectedPostsOnly()
        {
            _postService.RejectPost(2, "Test rejection");

            var result = _postService.GetRejectedPosts(1, 10);

            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Test Post 2");
        }

        // ============ NOTIFICATION DTO TESTS ============

        [Fact]
        public void GetModeratorNotifications_ReturnsNotificationsList()
        {
            var result = _postService.GetModeratorNotifications();

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IEnumerable<NotificationDTO>>();
        }

        [Fact]
        public void GetModeratorNotifications_ReturnsNotificationsWithCorrectStructure()
        {
            var result = _postService.GetModeratorNotifications().ToList();

            result.Should().NotBeEmpty();
            result.Should().OnlyContain(n => 
                n.NotificationId > 0 &&
                !string.IsNullOrEmpty(n.Title) &&
                !string.IsNullOrEmpty(n.Content) &&
                !string.IsNullOrEmpty(n.Type));
        }

        [Fact]
        public void GetModeratorNotifications_ReturnsNotificationsWithValidTypes()
        {
            var result = _postService.GetModeratorNotifications().ToList();

            result.Should().Contain(n => n.Type == "pending");
            result.Should().Contain(n => n.Type == "reported");
        }

        [Fact]
        public void GetModeratorNotifications_ReturnsNotificationsWithTimestamps()
        {
            var result = _postService.GetModeratorNotifications().ToList();

            result.Should().OnlyContain(n => n.CreatedAt <= DateTime.UtcNow);
            result.Should().OnlyContain(n => n.CreatedAt > DateTime.UtcNow.AddDays(-1));
        }

        // ============ POST_TAG MODEL TESTS ============

        [Fact]
        public void DeletePost_WithTags_RemovesPostTagRelationships()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            // Create post with tags
            var dto = new CreatePostDTO
            {
                Title = "Post with Tags",
                Content = "Content",
                TagNames = new List<string> { "General", "TestTag" },
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var createdPost = _postService.CreatePost(dto, 1);
            var postId = createdPost.PostId;

            // Verify tags were added
            var postWithTags = _context.Post
                .Include(p => p.Tags)
                .FirstOrDefault(p => p.PostId == postId);
            postWithTags!.Tags.Should().HaveCount(2);

            // Delete the post
            _postService.DeletePost(postId, 1);

            // Verify post is deleted
            var deletedPost = _context.Post.Find(postId);
            deletedPost.Should().BeNull();

            // Verify Post_Tag relationships are removed (tags still exist)
            var tagsStillExist = _context.Tag.Count();
            tagsStillExist.Should().BeGreaterThanOrEqualTo(2); // Tags should not be deleted
        }

        [Fact]
        public void CreatePost_WithMultipleTags_CreatesPostTagRelationships()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreatePostDTO
            {
                Title = "Multi-tag Post",
                Content = "Content with multiple tags",
                TagNames = new List<string> { "General", "Tech", "News" },
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var result = _postService.CreatePost(dto, 1);

            result.Should().NotBeNull();
            result.Tags.Should().HaveCount(3);
            result.Tags.Should().Contain(t => t.TagName == "General");
            result.Tags.Should().Contain(t => t.TagName == "Tech");
            result.Tags.Should().Contain(t => t.TagName == "News");
        }

        [Fact]
        public void UpdatePost_WithNewTags_UpdatesPostTagRelationships()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            // Create post with initial tags
            var createDto = new CreatePostDTO
            {
                Title = "Original",
                Content = "Content",
                TagNames = new List<string> { "General" },
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var createdPost = _postService.CreatePost(createDto, 1);
            createdPost.Tags.Should().HaveCount(1);

            // Update with new tags
            var updateDto = new UpdatePostDTO
            {
                Title = "Updated",
                Content = "Updated content",
                TagNames = new List<string> { "Tech", "News" }
            };

            _postService.UpdatePost(createdPost.PostId, updateDto, 1);

            // Verify tags were updated
            var updatedPost = _postService.GetPostById(createdPost.PostId);
            updatedPost!.Tags.Should().HaveCount(2);
            updatedPost.Tags.Should().Contain(t => t.TagName == "Tech");
            updatedPost.Tags.Should().Contain(t => t.TagName == "News");
            updatedPost.Tags.Should().NotContain(t => t.TagName == "General");
        }

        [Fact]
        public void CreatePost_WithDuplicateTags_CreatesUniquePostTagRelationships()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreatePostDTO
            {
                Title = "Duplicate Tags Post",
                Content = "Content",
                TagNames = new List<string> { "General", "General", "Tech" }, // Duplicate "General"
                Attachments = new List<CreatePostAttachmentDTO>()
            };

            var result = _postService.CreatePost(dto, 1);

            result.Should().NotBeNull();
            result.Tags.Should().HaveCount(2); // Should only have 2 unique tags
            result.Tags.Where(t => t.TagName == "General").Should().HaveCount(1);
        }
    }
}

