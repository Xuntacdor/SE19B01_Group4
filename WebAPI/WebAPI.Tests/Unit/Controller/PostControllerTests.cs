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
    public class PostControllerTests
    {
        private readonly Mock<IPostService> _postService;
        private readonly Mock<ICommentService> _commentService;
        private readonly PostController _controller;

        public PostControllerTests()
        {
            _postService = new Mock<IPostService>();
            _commentService = new Mock<ICommentService>();
            _controller = new PostController(_postService.Object, _commentService.Object);
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

        // ============ GET POSTS ============

        [Fact]
        public void GetPosts_ReturnsOkWithPosts()
        {
            var posts = new List<PostDTO>
            {
                new PostDTO { PostId = 1, Title = "Test Post", Content = "Content" }
            };
            _postService.Setup(s => s.GetPosts(1, 10, null)).Returns(posts);

            var result = _controller.GetPosts(1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(posts);
        }

        [Fact]
        public void GetPosts_WithLoggedInUser_ReturnsPosts()
        {
            SetSession(1);
            var posts = new List<PostDTO> { new PostDTO { PostId = 1 } };
            _postService.Setup(s => s.GetPosts(1, 10, 1)).Returns(posts);

            var result = _controller.GetPosts(1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        // ============ GET POSTS BY FILTER ============

        [Fact]
        public void GetPostsByFilter_ReturnsOk()
        {
            var posts = new List<PostDTO> { new PostDTO { PostId = 1 } };
            _postService.Setup(s => s.GetPostsByFilter("new", 1, 10, null)).Returns(posts);

            var result = _controller.GetPostsByFilter("new", 1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetPostsByFilter_WithTopFilter_ReturnsOk()
        {
            var posts = new List<PostDTO> { new PostDTO { PostId = 1 } };
            _postService.Setup(s => s.GetPostsByFilter("top", 1, 10, null)).Returns(posts);

            var result = _controller.GetPostsByFilter("top", 1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        // ============ GET POST BY ID ============

        [Fact]
        public void GetPost_WhenFound_ReturnsOk()
        {
            var post = new PostDTO { PostId = 1, Title = "Test" };
            _postService.Setup(s => s.GetPostById(1, null)).Returns(post);

            var result = _controller.GetPost(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            _postService.Verify(s => s.IncrementViewCount(1), Times.Once);
        }

        [Fact]
        public void GetPost_WithLoggedInUser_ReturnsOk()
        {
            SetSession(1);
            var post = new PostDTO { PostId = 1, Title = "Test" };
            _postService.Setup(s => s.GetPostById(It.IsAny<int>(), It.IsAny<int?>())).Returns(post);

            var result = _controller.GetPost(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            _postService.Verify(s => s.IncrementViewCount(1), Times.Once);
        }

        [Fact]
        public void GetPost_WhenNotFound_ReturnsNotFound()
        {
            _postService.Setup(s => s.GetPostById(999, null)).Returns((PostDTO?)null);

            var result = _controller.GetPost(999);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Post not found");
        }

        // ============ CREATE POST ============

        [Fact]
        public void CreatePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();
            var dto = new CreatePostDTO { Title = "Test", Content = "Content" };

            var result = _controller.CreatePost(dto);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result.Result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Please login to create posts");
        }

        [Fact]
        public void CreatePost_WhenLoggedIn_ReturnsCreated()
        {
            SetSession(1);
            var dto = new CreatePostDTO { Title = "Test", Content = "Content" };
            var created = new PostDTO { PostId = 1, Title = "Test", Content = "Content" };
            _postService.Setup(s => s.CreatePost(It.IsAny<CreatePostDTO>(), It.IsAny<int>()))
                        .Returns(created);

            var result = _controller.CreatePost(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void CreatePost_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            var dto = new CreatePostDTO { Title = "Test", Content = "Test" };
            _postService.Setup(s => s.CreatePost(It.IsAny<CreatePostDTO>(), It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.CreatePost(dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Error");
        }

        // ============ UPDATE POST ============

        [Fact]
        public void UpdatePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();
            var dto = new UpdatePostDTO { Title = "Updated" };

            var result = _controller.UpdatePost(1, dto);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UpdatePost_WhenSuccessful_ReturnsNoContent()
        {
            SetSession(1);
            var dto = new UpdatePostDTO { Title = "Updated" };

            var result = _controller.UpdatePost(1, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void UpdatePost_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            var dto = new UpdatePostDTO { Title = "Updated" };
            _postService.Setup(s => s.UpdatePost(It.IsAny<int>(), It.IsAny<UpdatePostDTO>(), It.IsAny<int>()))
                        .Throws<KeyNotFoundException>();

            var result = _controller.UpdatePost(999, dto);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Post not found");
        }

        [Fact]
        public void UpdatePost_WhenUnauthorized_ReturnsForbid()
        {
            SetSession(1);
            var dto = new UpdatePostDTO { Title = "Updated" };
            _postService.Setup(s => s.UpdatePost(It.IsAny<int>(), It.IsAny<UpdatePostDTO>(), It.IsAny<int>()))
                        .Throws<UnauthorizedAccessException>();

            var result = _controller.UpdatePost(1, dto);

            result.Should().BeOfType<ForbidResult>();
        }

        // ============ DELETE POST ============

        [Fact]
        public void DeletePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.DeletePost(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void DeletePost_WhenSuccessful_ReturnsNoContent()
        {
            SetSession(1);

            var result = _controller.DeletePost(1);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void DeletePost_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _postService.Setup(s => s.DeletePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new KeyNotFoundException());

            var result = _controller.DeletePost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Post not found");
        }

        [Fact]
        public void DeletePost_WhenUnauthorized_ReturnsForbid()
        {
            SetSession(1);
            _postService.Setup(s => s.DeletePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new UnauthorizedAccessException());

            var result = _controller.DeletePost(1);

            result.Should().BeOfType<ForbidResult>();
        }

        // ============ VOTE POST ============

        [Fact]
        public void VotePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.VotePost(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void VotePost_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.VotePost(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void VotePost_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _postService.Setup(s => s.VotePost(999, It.IsAny<int>()))
                        .Throws(new KeyNotFoundException("Post not found"));

            var result = _controller.VotePost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Post not found");

            _postService.Verify(s => s.VotePost(999, It.IsAny<int>()), Times.Once);
        }


        // ============ UNVOTE POST ============

        [Fact]
        public void UnvotePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.UnvotePost(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UnvotePost_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.UnvotePost(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void UnvotePost_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _postService.Setup(s => s.UnvotePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new KeyNotFoundException());

            var result = _controller.UnvotePost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UnvotePost_WhenInvalidOperation_ReturnsBadRequest()
        {
            SetSession(1);
            _postService.Setup(s => s.UnvotePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new InvalidOperationException("Already unvoted"));

            var result = _controller.UnvotePost(1);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Already unvoted");
        }

        // ============ GET COMMENTS ============

        [Fact]
        public void GetCommentsByPostId_ReturnsOk()
        {
            var comments = new List<CommentDTO> { new CommentDTO { CommentId = 1 } };
            _commentService.Setup(s => s.GetCommentsByPostId(It.IsAny<int>(), It.IsAny<int?>())).Returns(comments);

            var result = _controller.GetCommentsByPostId(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(comments);
        }

        [Fact]
        public void GetCommentsByPostId_WithEmptyList_ReturnsOk()
        {
            var comments = new List<CommentDTO>();
            _commentService.Setup(s => s.GetCommentsByPostId(It.IsAny<int>(), It.IsAny<int?>())).Returns(comments);

            var result = _controller.GetCommentsByPostId(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            var returnedComments = ok!.Value as List<CommentDTO>;
            returnedComments.Should().BeEmpty();
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
            var comment = new CommentDTO { CommentId = 1 };
            _commentService.Setup(s => s.CreateComment(1, dto, 1)).Returns(comment);

            var result = _controller.CreateComment(1, dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
        }

        [Fact]
        public void CreateComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            var dto = new CreateCommentDTO { Content = "Comment" };
            _commentService.Setup(s => s.CreateComment(It.IsAny<int>(), It.IsAny<CreateCommentDTO>(), It.IsAny<int>()))
                            .Throws(new KeyNotFoundException("Post not found"));

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
            _commentService.Setup(s => s.CreateComment(It.IsAny<int>(), It.IsAny<CreateCommentDTO>(), It.IsAny<int>()))
                            .Throws(new Exception("Error"));

            var result = _controller.CreateComment(1, dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().Be("Error");
        }

        // ============ DELETE COMMENT ============

        [Fact]
        public void DeleteComment_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.DeleteComment(1, 1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void DeleteComment_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.DeleteComment(1, 1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void DeleteComment_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _commentService.Setup(s => s.DeleteComment(It.IsAny<int>(), It.IsAny<int>()))
                            .Throws(new KeyNotFoundException("Comment not found"));

            var result = _controller.DeleteComment(1, 999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Comment not found");
        }

        [Fact]
        public void DeleteComment_WhenUnauthorized_ReturnsForbid()
        {
            SetSession(1);
            _commentService.Setup(s => s.DeleteComment(It.IsAny<int>(), It.IsAny<int>()))
                            .Throws(new UnauthorizedAccessException("Forbidden"));

            var result = _controller.DeleteComment(1, 1);

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void DeleteComment_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            _commentService.Setup(s => s.DeleteComment(It.IsAny<int>(), It.IsAny<int>()))
                            .Throws(new Exception("Error"));

            var result = _controller.DeleteComment(1, 1);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Error");
        }

        // ============ PIN POST ============

        [Fact]
        public void PinPost_WhenSuccessful_ReturnsOk()
        {
            var result = _controller.PinPost(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void PinPost_WhenNotFound_ReturnsNotFound()
        {
            _postService.Setup(s => s.PinPost(999)).Throws(new KeyNotFoundException("Post not found"));

            var result = _controller.PinPost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void PinPost_WhenException_ReturnsBadRequest()
        {
            _postService.Setup(s => s.PinPost(It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.PinPost(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ============ UNPIN POST ============

        [Fact]
        public void UnpinPost_WhenSuccessful_ReturnsOk()
        {
            var result = _controller.UnpinPost(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void UnpinPost_WhenNotFound_ReturnsNotFound()
        {
            _postService.Setup(s => s.UnpinPost(It.IsAny<int>()))
                        .Throws(new KeyNotFoundException("Post not found"));

            var result = _controller.UnpinPost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UnpinPost_WhenException_ReturnsBadRequest()
        {
            _postService.Setup(s => s.UnpinPost(It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.UnpinPost(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ============ HIDE POST ============

        [Fact]
        public void HidePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.HidePost(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void HidePost_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.HidePost(1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void HidePost_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _postService.Setup(s => s.HidePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new KeyNotFoundException("Post not found"));

            var result = _controller.HidePost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void HidePost_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            _postService.Setup(s => s.HidePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.HidePost(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ============ UNHIDE POST ============

        [Fact]
        public void UnhidePost_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.UnhidePost(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UnhidePost_WhenSuccessful_ReturnsOk()
        {
            SetSession(1);

            var result = _controller.UnhidePost(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Post unhidden successfully" });
        }

        [Fact]
        public void UnhidePost_WhenNotFound_ReturnsNotFound()
        {
            SetSession(1);
            _postService.Setup(s => s.UnhidePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new KeyNotFoundException("Post not found"));

            var result = _controller.UnhidePost(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void UnhidePost_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            _postService.Setup(s => s.UnhidePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.UnhidePost(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ============ EXTRA: UPDATE EXCEPTION & VOTE EXCEPTION ============

        [Fact]
        public void UpdatePost_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            var dto = new UpdatePostDTO { Title = "Updated" };
            _postService.Setup(s => s.UpdatePost(It.IsAny<int>(), It.IsAny<UpdatePostDTO>(), It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.UpdatePost(1, dto);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Error");
        }

        [Fact]
        public void UpdatePost_WhenGenericException_ReturnsBadRequest()
        {
            SetSession(1);
            var dto = new UpdatePostDTO { Title = "Updated" };
            _postService.Setup(s => s.UpdatePost(It.IsAny<int>(), It.IsAny<UpdatePostDTO>(), It.IsAny<int>()))
                        .Throws(new InvalidOperationException("Database error"));

            var result = _controller.UpdatePost(1, dto);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Database error");
        }

        [Fact]
        public void VotePost_WhenException_ReturnsBadRequest()
        {
            SetSession(1);
            _postService.Setup(s => s.VotePost(It.IsAny<int>(), It.IsAny<int>()))
                        .Throws(new Exception("Error"));

            var result = _controller.VotePost(1);

            result.Should().BeOfType<BadRequestObjectResult>();
            var bad = result as BadRequestObjectResult;
            bad!.Value.Should().Be("Error");
        }

        // ============ ADDITIONAL EDGE CASES ============

        // ============ EDGE CASES & ADDITIONAL SCENARIOS ============

        [Fact]
        public void GetPosts_WithCustomPagination_ReturnsOk()
        {
            var posts = new List<PostDTO>();
            for (int i = 1; i <= 50; i++)
            {
                posts.Add(new PostDTO { PostId = i, Title = $"Post {i}" });
            }
            _postService.Setup(s => s.GetPosts(5, 50, null)).Returns(posts);

            var result = _controller.GetPosts(5, 50);

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            var returnedPosts = ok!.Value as List<PostDTO>;
            returnedPosts.Should().HaveCount(50);
        }

        [Fact]
        public void GetPostsByFilter_WithClosedFilter_ReturnsOk()
        {
            var posts = new List<PostDTO> { new PostDTO { PostId = 1, Title = "Closed Post" } };
            _postService.Setup(s => s.GetPostsByFilter("closed", 1, 10, null)).Returns(posts);

            var result = _controller.GetPostsByFilter("closed", 1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
            _postService.Verify(s => s.GetPostsByFilter("closed", 1, 10, null), Times.Once);
        }

        [Fact]
        public void CreatePost_WithComplexDTO_ReturnsCreated()
        {
            SetSession(1);
            var dto = new CreatePostDTO 
            { 
                Title = "Complex Post",
                Content = "This is a detailed post with multiple fields",
            };
            var created = new PostDTO 
            { 
                PostId = 100, 
                Title = "Complex Post",
                Content = "This is a detailed post with multiple fields"
            };
            _postService.Setup(s => s.CreatePost(It.IsAny<CreatePostDTO>(), It.IsAny<int>())).Returns(created);

            var result = _controller.CreatePost(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result.Result as CreatedAtActionResult;
            var returnedPost = createdResult!.Value as PostDTO;
            returnedPost!.PostId.Should().Be(100);
            returnedPost.Title.Should().Be("Complex Post");
        }

        [Fact]
        public void UpdatePost_WithPartialData_ReturnsNoContent()
        {
            SetSession(1);
            var dto = new UpdatePostDTO { Content = "Updated content only" };

            var result = _controller.UpdatePost(1, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void GetPost_VerifiesIncrementViewCountCalled()
        {
            var post = new PostDTO { PostId = 1, Title = "Test", ViewCount = 5 };
            _postService.Setup(s => s.GetPostById(1, null)).Returns(post);

            var result = _controller.GetPost(1);

            _postService.Verify(s => s.IncrementViewCount(1), Times.Once);
            _postService.Verify(s => s.GetPostById(1, null), Times.Once);
        }

        [Fact]
        public void VotePost_WithValidId_ReturnsSuccessMessage()
        {
            SetSession(1);

            var result = _controller.VotePost(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().Be("Vote successful");
        }

        [Fact]
        public void UnvotePost_WithValidId_ReturnsSuccessMessage()
        {
            SetSession(1);

            var result = _controller.UnvotePost(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().Be("Post unvoted successfully");
        }

        [Fact]
        public void PinPost_ReturnsSuccessMessage()
        {
            var result = _controller.PinPost(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Post pinned successfully" });
        }

        [Fact]
        public void UnpinPost_ReturnsSuccessMessage()
        {
            var result = _controller.UnpinPost(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Post unpinned successfully" });
        }

        [Fact]
        public void HidePost_ReturnsSuccessMessage()
        {
            SetSession(1);

            var result = _controller.HidePost(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Post hidden successfully" });
        }

        [Fact]
        public void DeleteComment_ReturnsSuccessMessage()
        {
            SetSession(1);

            var result = _controller.DeleteComment(1, 1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Comment deleted successfully" });
        }
    }

    // Helper class for testing
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }
}

