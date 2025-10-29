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
    public class CommentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly CommentService _commentService;

        public CommentServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _userRepositoryMock = new Mock<IUserRepository>();
            _commentService = new CommentService(_context, _userRepositoryMock.Object);

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

            var post = new Post
            {
                PostId = 1,
                UserId = 1,
                Title = "Test Post",
                Content = "Test Content",
                Status = "approved",
                CreatedAt = DateTime.UtcNow
            };

            _context.Post.Add(post);

            var comment1 = new Comment
            {
                CommentId = 1,
                PostId = 1,
                UserId = 1,
                Content = "Root comment",
                ParentCommentId = null,
                CreatedAt = DateTime.UtcNow
            };

            var comment2 = new Comment
            {
                CommentId = 2,
                PostId = 1,
                UserId = 2,
                Content = "Reply to comment 1",
                ParentCommentId = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            };

            _context.Comment.AddRange(comment1, comment2);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============ GET COMMENTS BY POST ID ============

        [Fact]
        public void GetCommentsByPostId_ReturnsRootCommentsWithReplies()
        {
            var result = _commentService.GetCommentsByPostId(1);

            result.Should().NotBeNull();
            result.Should().HaveCount(1); // Only root comment
            result.First().Replies.Should().HaveCount(1); // One reply
        }

        [Fact]
        public void GetCommentsByPostId_NonExistentPost_ReturnsEmpty()
        {
            var result = _commentService.GetCommentsByPostId(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GetCommentsByPostId_WithUserId_IncludesVoteStatus()
        {
            // Add a like
            _context.CommentLike.Add(new CommentLike
            {
                CommentId = 1,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = _commentService.GetCommentsByPostId(1, 1);

            result.First().IsVoted.Should().BeTrue();
        }

        // ============ GET COMMENT BY ID ============

        [Fact]
        public void GetCommentById_ExistingComment_ReturnsComment()
        {
            var result = _commentService.GetCommentById(1);

            result.Should().NotBeNull();
            result!.CommentId.Should().Be(1);
            result.Content.Should().Be("Root comment");
        }

        [Fact]
        public void GetCommentById_NonExistentComment_ReturnsNull()
        {
            var result = _commentService.GetCommentById(999);

            result.Should().BeNull();
        }

        // ============ CREATE COMMENT ============

        [Fact]
        public void CreateComment_ValidData_CreatesComment()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreateCommentDTO
            {
                Content = "New comment",
                ParentCommentId = null
            };

            var result = _commentService.CreateComment(1, dto, 1);

            result.Should().NotBeNull();
            result.Content.Should().Be("New comment");
            result.ParentCommentId.Should().BeNull();
        }

        [Fact]
        public void CreateComment_NonExistentPost_ThrowsException()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreateCommentDTO { Content = "Comment" };

            Action act = () => _commentService.CreateComment(999, dto, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Post not found");
        }

        [Fact]
        public void CreateComment_NonExistentUser_ThrowsException()
        {
            _userRepositoryMock.Setup(u => u.GetById(999)).Returns((User?)null);

            var dto = new CreateCommentDTO { Content = "Comment" };

            Action act = () => _commentService.CreateComment(1, dto, 999);

            act.Should().Throw<KeyNotFoundException>().WithMessage("User not found");
        }

        // ============ CREATE REPLY ============

        [Fact]
        public void CreateReply_ValidData_CreatesReply()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreateCommentDTO
            {
                Content = "Reply to root comment"
            };

            var result = _commentService.CreateReply(1, dto, 1);

            result.Should().NotBeNull();
            result.Content.Should().Be("Reply to root comment");
            result.ParentCommentId.Should().Be(1);
        }

        [Fact]
        public void CreateReply_NonExistentParent_ThrowsException()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new CreateCommentDTO { Content = "Reply" };

            Action act = () => _commentService.CreateReply(999, dto, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Parent comment not found");
        }

        // ============ UPDATE COMMENT ============

        [Fact]
        public void UpdateComment_ValidData_UpdatesComment()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            var dto = new UpdateCommentDTO
            {
                Content = "Updated comment"
            };

            _commentService.UpdateComment(1, dto, 1);

            var updated = _context.Comment.Find(1);
            updated!.Content.Should().Be("Updated comment");
        }

        [Fact]
        public void UpdateComment_NonExistentComment_ThrowsException()
        {
            var dto = new UpdateCommentDTO { Content = "Updated" };

            Action act = () => _commentService.UpdateComment(999, dto, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Comment not found");
        }

        [Fact]
        public void UpdateComment_UnauthorizedUser_ThrowsException()
        {
            var user3 = new User
            {
                UserId = 3,
                Username = "other",
                Email = "other@test.com",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 1 },
                Role = "user"
            };
            _context.User.Add(user3);
            _context.SaveChanges();

            _userRepositoryMock.Setup(u => u.GetById(3)).Returns(user3);

            var dto = new UpdateCommentDTO { Content = "Updated" };

            Action act = () => _commentService.UpdateComment(1, dto, 3);

            act.Should().Throw<UnauthorizedAccessException>();
        }

        [Fact]
        public void UpdateComment_AdminUser_CanUpdateAnyComment()
        {
            _userRepositoryMock.Setup(u => u.GetById(2)).Returns(_context.User.Find(2)!);

            var dto = new UpdateCommentDTO { Content = "Admin updated" };

            _commentService.UpdateComment(1, dto, 2);

            var updated = _context.Comment.Find(1);
            updated!.Content.Should().Be("Admin updated");
        }

        // ============ DELETE COMMENT ============

        [Fact]
        public void DeleteComment_CommentOwner_DeletesComment()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            _commentService.DeleteComment(1, 1);

            var deleted = _context.Comment.Find(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public void DeleteComment_AdminUser_CanDeleteAnyComment()
        {
            _userRepositoryMock.Setup(u => u.GetById(2)).Returns(_context.User.Find(2)!);

            _commentService.DeleteComment(1, 2);

            var deleted = _context.Comment.Find(1);
            deleted.Should().BeNull();
        }

        [Fact]
        public void DeleteComment_PostOwner_CanDeleteCommentOnTheirPost()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            // User 1 is post owner, deleting comment from user 2
            _commentService.DeleteComment(2, 1);

            var deleted = _context.Comment.Find(2);
            deleted.Should().BeNull();
        }

        [Fact]
        public void DeleteComment_UnauthorizedUser_ThrowsException()
        {
            var user3 = new User
            {
                UserId = 3,
                Username = "other",
                Email = "other@test.com",
                PasswordHash = new byte[] { 1 },
                PasswordSalt = new byte[] { 1 },
                Role = "user"
            };
            _context.User.Add(user3);
            _context.SaveChanges();

            _userRepositoryMock.Setup(u => u.GetById(3)).Returns(user3);

            Action act = () => _commentService.DeleteComment(1, 3);

            act.Should().Throw<UnauthorizedAccessException>();
        }

        [Fact]
        public void DeleteComment_NonExistentComment_ThrowsException()
        {
            Action act = () => _commentService.DeleteComment(999, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Comment not found");
        }

        [Fact]
        public void DeleteComment_WithReplies_DeletesAllReplies()
        {
            _userRepositoryMock.Setup(u => u.GetById(1)).Returns(_context.User.Find(1)!);

            // Comment 1 has reply (comment 2)
            _commentService.DeleteComment(1, 1);

            var deletedParent = _context.Comment.Find(1);
            var deletedReply = _context.Comment.Find(2);

            deletedParent.Should().BeNull();
            deletedReply.Should().BeNull();
        }

        // ============ LIKE COMMENT ============

        [Fact]
        public void LikeComment_ValidComment_AddsLike()
        {
            _commentService.LikeComment(1, 2);

            var like = _context.CommentLike.FirstOrDefault(cl => cl.CommentId == 1 && cl.UserId == 2);
            like.Should().NotBeNull();
        }

        [Fact]
        public void LikeComment_AlreadyLiked_ThrowsException()
        {
            _context.CommentLike.Add(new CommentLike
            {
                CommentId = 1,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            Action act = () => _commentService.LikeComment(1, 1);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You have already liked this comment");
        }

        [Fact]
        public void LikeComment_NonExistentComment_ThrowsException()
        {
            Action act = () => _commentService.LikeComment(999, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Comment not found");
        }

        // ============ UNLIKE COMMENT ============

        [Fact]
        public void UnlikeComment_ValidLike_RemovesLike()
        {
            _context.CommentLike.Add(new CommentLike
            {
                CommentId = 1,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            _commentService.UnlikeComment(1, 1);

            var like = _context.CommentLike.FirstOrDefault(cl => cl.CommentId == 1 && cl.UserId == 1);
            like.Should().BeNull();
        }

        [Fact]
        public void UnlikeComment_NotLiked_ThrowsException()
        {
            Action act = () => _commentService.UnlikeComment(1, 1);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("You haven't liked this comment");
        }

        [Fact]
        public void UnlikeComment_NonExistentComment_ThrowsException()
        {
            Action act = () => _commentService.UnlikeComment(999, 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Comment not found");
        }

        // ============ NESTED COMMENTS ============

        [Fact]
        public void GetCommentsByPostId_WithNestedReplies_ReturnsHierarchy()
        {
            // Add a reply to comment 2 (which is already a reply to comment 1)
            var comment3 = new Comment
            {
                CommentId = 3,
                PostId = 1,
                UserId = 1,
                Content = "Reply to reply",
                ParentCommentId = 2,
                CreatedAt = DateTime.UtcNow.AddMinutes(2)
            };
            _context.Comment.Add(comment3);
            _context.SaveChanges();

            var result = _commentService.GetCommentsByPostId(1);

            result.Should().HaveCount(1); // Still only 1 root comment
            result.First().Replies.Should().HaveCount(1); // One reply
            result.First().Replies.First().Replies.Should().HaveCount(1); // Nested reply
        }

        // ============ COMMENT LIKES COUNT ============

        [Fact]
        public void GetCommentById_WithLikes_ReturnsCorrectCount()
        {
            _context.CommentLike.Add(new CommentLike
            {
                CommentId = 1,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            });
            _context.CommentLike.Add(new CommentLike
            {
                CommentId = 1,
                UserId = 2,
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();

            var result = _commentService.GetCommentById(1);

            result!.VoteCount.Should().Be(2);
            result.LikeNumber.Should().Be(2);
        }

        // ============ REPORT COMMENT ============

        [Fact]
        public void ReportComment_ValidComment_CreatesReport()
        {
            _commentService.ReportComment(1, "Spam content", 2);

            var report = _context.Report.FirstOrDefault(r => r.CommentId == 1 && r.UserId == 2);
            report.Should().NotBeNull();
            report!.Content.Should().Be("Spam content");
            report.Status.Should().Be("Pending");
            report.CommentAuthorUserId.Should().Be(1); // Comment 1 is owned by user 1
        }

        [Fact]
        public void ReportComment_NonExistentComment_ThrowsException()
        {
            Action act = () => _commentService.ReportComment(999, "Spam", 1);

            act.Should().Throw<KeyNotFoundException>().WithMessage("Comment not found");
        }

        [Fact]
        public void ReportComment_MultipleReports_AllSaved()
        {
            _commentService.ReportComment(1, "Inappropriate", 1);
            _commentService.ReportComment(1, "Spam", 2);

            var reports = _context.Report.Where(r => r.CommentId == 1).ToList();
            reports.Should().HaveCount(2);
            reports.Should().Contain(r => r.UserId == 1 && r.Content == "Inappropriate");
            reports.Should().Contain(r => r.UserId == 2 && r.Content == "Spam");
        }

        [Fact]
        public void ReportComment_SetsCorrectCreatedAt()
        {
            var beforeReport = DateTime.UtcNow;
            _commentService.ReportComment(1, "Test report", 2);
            var afterReport = DateTime.UtcNow;

            var report = _context.Report.FirstOrDefault(r => r.CommentId == 1 && r.UserId == 2);
            report.Should().NotBeNull();
            report!.CreatedAt.Should().BeOnOrAfter(beforeReport);
            report.CreatedAt.Should().BeOnOrBefore(afterReport);
        }

        [Fact]
        public void ReportComment_StoresCommentAuthorUserId()
        {
            // Comment 2 is owned by user 2
            _commentService.ReportComment(2, "Offensive language", 1);

            var report = _context.Report.FirstOrDefault(r => r.CommentId == 2);
            report.Should().NotBeNull();
            report!.CommentAuthorUserId.Should().Be(2);
        }
    }
}

