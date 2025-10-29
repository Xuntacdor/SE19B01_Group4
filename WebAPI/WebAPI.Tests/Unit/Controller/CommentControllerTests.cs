using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class CommentControllerTests
    {
        private readonly Mock<ICommentService> _commentService;
        private readonly CommentController _controller;

        public CommentControllerTests()
        {
            _commentService = new Mock<ICommentService>();
            _controller = new CommentController(_commentService.Object);
            // Set up default HttpContext with session
            ClearSession();
        }

        private void SetSession(int? userId = 1)
        {
            var context = new DefaultHttpContext();
            var session = new TestSession();
            context.Session = session;
            if (userId.HasValue)
            {
                // Use proper byte conversion for Int32
                var bytes = new byte[4];
                bytes[0] = (byte)(userId.Value & 0xFF);
                bytes[1] = (byte)((userId.Value >> 8) & 0xFF);
                bytes[2] = (byte)((userId.Value >> 16) & 0xFF);
                bytes[3] = (byte)((userId.Value >> 24) & 0xFF);
                session.Set("UserId", bytes);
            }
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        private void ClearSession()
        {
            var context = new DefaultHttpContext();
            context.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        // ============ GET COMMENTS BY POST ============

        [Fact]
        public void GetCommentsByPostId_ReturnsOk()
        {
            var comments = new List<CommentDTO> { new CommentDTO { CommentId = 1, Content = "Test" } };
            _commentService.Setup(s => s.GetCommentsByPostId(1, null)).Returns(comments);

            var result = _controller.GetCommentsByPostId(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(comments);
        }

        [Fact]
        public void GetCommentsByPostId_WithLoggedInUser_ReturnsOk()
        {
            SetSession(1);
            var comments = new List<CommentDTO> { new CommentDTO { CommentId = 1 } };
            _commentService.Setup(s => s.GetCommentsByPostId(1, 1)).Returns(comments);

            var result = _controller.GetCommentsByPostId(1);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        // ============ GET COMMENT BY ID ============

        [Fact]
        public void GetComment_WhenFound_ReturnsOk()
        {
            var comment = new CommentDTO { CommentId = 1, Content = "Test" };
            _commentService.Setup(s => s.GetCommentById(1, null))
                           .Returns(comment);

            var result = _controller.GetComment(1);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _commentService.Setup(s => s.GetCommentById(999, 1)).Returns((CommentDTO?)null);

            var result = _controller.GetComment(999);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Comment not found");
        }

        // ============ CREATE COMMENT ============

        [Fact]
        public void CreateComment_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();
            var dto = new CreateCommentDTO { Content = "Comment" };

            var result = _controller.CreateComment(1, dto);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void CreateComment_WhenSuccessful_ReturnsCreated()
        {
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Comment" };
            var comment = new CommentDTO { CommentId = 1, Content = "Comment" };
            _commentService.Setup(s => s.CreateComment(It.IsAny<int>(), It.IsAny<CreateCommentDTO>(), It.IsAny<int>()))
                           .Returns(comment);

            var result = _controller.CreateComment(1, dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void CreateComment_WhenPostNotFound_ReturnsNotFound()
        {
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Comment" };

            _commentService
          .Setup(s => s.CreateComment(999, It.IsAny<CreateCommentDTO>(), 1))
          .Returns(() => throw new KeyNotFoundException("Post not found"));
            var result = _controller.CreateComment(999, dto);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Post not found");
        }
        [Fact]
        public void CreateComment_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Comment" };
            _commentService
                .Setup(s => s.CreateComment(It.IsAny<int>(), It.IsAny<CreateCommentDTO>(), It.IsAny<int>()))
                .Throws(new Exception("Error"));

            var result = _controller.CreateComment(1, dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ============ CREATE REPLY ============

        [Fact]
        public void CreateReply_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();
            var dto = new CreateCommentDTO { Content = "Reply" };

            var result = _controller.CreateReply(1, dto);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void CreateReply_WhenSuccessful_ReturnsCreated()
        {
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Reply" };
            var comment = new CommentDTO { CommentId = 2, Content = "Reply" };
            _commentService.Setup(s => s.CreateReply(It.IsAny<int>(), It.IsAny<CreateCommentDTO>(), It.IsAny<int>()))
                           .Returns(comment);

            var result = _controller.CreateReply(1, dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void CreateReply_WhenParentNotFound_ReturnsNotFound()
        {
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Reply" };

            _commentService
         .Setup(s => s.CreateReply(999, It.IsAny<CreateCommentDTO>(), 1))
         .Returns(() => throw new KeyNotFoundException("Comment not found"));

            var result = _controller.CreateReply(999, dto);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Comment not found");
        }


        // ============ UPDATE COMMENT ============

        [Fact]
        public void UpdateComment_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();
            var dto = new UpdateCommentDTO { Content = "Updated" };

            var result = _controller.UpdateComment(1, dto);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UpdateComment_WhenSuccessful_ReturnsNoContent()
        {
            SetSession(1);
            var dto = new UpdateCommentDTO { Content = "Updated" };

            var result = _controller.UpdateComment(1, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void UpdateComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            var dto = new UpdateCommentDTO { Content = "Updated" };
            _commentService.Setup(s => s.UpdateComment(It.IsAny<int>(), It.IsAny<UpdateCommentDTO>(), It.IsAny<int>()))
                           .Throws(new KeyNotFoundException("Comment not found"));

            var result = _controller.UpdateComment(999, dto);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UpdateComment_WhenUnauthorized_ReturnsForbid()
        {
            SetSession(1);
            var dto = new UpdateCommentDTO { Content = "Updated" };
            // The controller returns Forbid() not ObjectResult
            _commentService.Setup(s => s.UpdateComment(It.IsAny<int>(), It.IsAny<UpdateCommentDTO>(), It.IsAny<int>()))
                           .Throws(new UnauthorizedAccessException("Not authorized"));

            var result = _controller.UpdateComment(1, dto);

            result.Should().BeOfType<ForbidResult>();
        }

        // ============ DELETE COMMENT ============

        [Fact]
        public void DeleteComment_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.DeleteComment(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void DeleteComment_WhenSuccessful_ReturnsNoContent()
        {
            SetSession(1);

            var result = _controller.DeleteComment(1);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void DeleteComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _commentService.Setup(s => s.DeleteComment(It.IsAny<int>(), It.IsAny<int>()))
                           .Throws(new KeyNotFoundException("Comment not found"));

            var result = _controller.DeleteComment(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void DeleteComment_WhenUnauthorized_ReturnsForbid()
        {
            SetSession(1);
            // The controller returns Forbid() not ObjectResult
            _commentService.Setup(s => s.DeleteComment(It.IsAny<int>(), It.IsAny<int>()))
                           .Throws(new UnauthorizedAccessException("Not authorized"));

            var result = _controller.DeleteComment(1);

            result.Should().BeOfType<ForbidResult>();
        }

        // ============ LIKE COMMENT ============

        [Fact]
        public void LikeComment_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.LikeComment(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void LikeComment_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.LikeComment(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().Be("Comment liked successfully");
        }

        [Fact]
        public void LikeComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _commentService.Setup(s => s.LikeComment(It.IsAny<int>(), It.IsAny<int>()))
                           .Throws(new KeyNotFoundException("Comment not found"));

            var result = _controller.LikeComment(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void LikeComment_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            _commentService.Setup(s => s.LikeComment(It.IsAny<int>(), It.IsAny<int>()))
                           .Throws(new Exception("Error"));

            var result = _controller.LikeComment(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ============ UNLIKE COMMENT ============

        [Fact]
        public void UnlikeComment_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.UnlikeComment(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UnlikeComment_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.UnlikeComment(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().Be("Comment unliked successfully");
        }

        [Fact]
        public void UnlikeComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _commentService.Setup(s => s.UnlikeComment(It.IsAny<int>(), It.IsAny<int>()))
                           .Throws(new KeyNotFoundException("Comment not found"));

            var result = _controller.UnlikeComment(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UnlikeComment_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            _commentService.Setup(s => s.UnlikeComment(It.IsAny<int>(), It.IsAny<int>()))
                           .Throws(new Exception("Error"));

            var result = _controller.UnlikeComment(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }
        [Fact]
        public void CreateComment_WhenWrappedKeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Comment" };

            var inner = new KeyNotFoundException("Inner Post not found");
            var wrapped = new Exception("Wrapper", inner);

            _commentService
                .Setup(s => s.CreateComment(It.IsAny<int>(), It.IsAny<CreateCommentDTO>(), It.IsAny<int>()))
                .Throws(wrapped);

            // Act
            var result = _controller.CreateComment(1, dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Wrapper");
        }

    }
}

