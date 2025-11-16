using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class TagControllerTests
    {
        private readonly Mock<ITagService> _tagServiceMock;
        private readonly TagController _controller;

        public TagControllerTests()
        {
            _tagServiceMock = new Mock<ITagService>();
            _controller = new TagController(_tagServiceMock.Object);
        }

        // ============ GET ALL TAGS ============

        [Fact]
        public void GetAllTags_WhenSuccessful_ReturnsOkWithTags()
        {
            var tags = new List<TagDTO>
            {
                new TagDTO { TagId = 1, TagName = "General", CreatedAt = DateTime.UtcNow, PostCount = 5 },
                new TagDTO { TagId = 2, TagName = "Question", CreatedAt = DateTime.UtcNow, PostCount = 3 }
            };
            _tagServiceMock.Setup(s => s.GetAllTags()).Returns(tags);

            var result = _controller.GetAllTags();

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public void GetAllTags_WhenEmpty_ReturnsOkWithEmptyList()
        {
            var tags = new List<TagDTO>();
            _tagServiceMock.Setup(s => s.GetAllTags()).Returns(tags);

            var result = _controller.GetAllTags();

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedTags = okResult!.Value as List<TagDTO>;
            returnedTags.Should().BeEmpty();
        }

        [Fact]
        public void GetAllTags_WhenException_ReturnsStatusCode500()
        {
            _tagServiceMock.Setup(s => s.GetAllTags()).Throws(new Exception("Database error"));

            var result = _controller.GetAllTags();

            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        // ============ GET TAG BY ID ============

        [Fact]
        public void GetTagById_WhenFound_ReturnsOkWithTag()
        {
            var tag = new TagDTO { TagId = 1, TagName = "General", CreatedAt = DateTime.UtcNow, PostCount = 5 };
            _tagServiceMock.Setup(s => s.GetTagById(1)).Returns(tag);

            var result = _controller.GetTagById(1);

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(tag);
        }

        [Fact]
        public void GetTagById_WhenNotFound_ReturnsNotFound()
        {
            _tagServiceMock.Setup(s => s.GetTagById(999)).Returns((TagDTO?)null);

            var result = _controller.GetTagById(999);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Tag not found" });
        }

        [Fact]
        public void GetTagById_WhenException_ReturnsStatusCode500()
        {
            _tagServiceMock.Setup(s => s.GetTagById(1)).Throws(new Exception("Database error"));

            var result = _controller.GetTagById(1);

            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        // ============ CREATE TAG ============

        [Fact]
        public void CreateTag_WhenValid_ReturnsCreatedAtAction()
        {
            var createDto = new CreateTagDTO { TagName = "NewTag" };
            var createdTag = new TagDTO { TagId = 1, TagName = "NewTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("NewTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.CreateTag(createDto)).Returns(createdTag);

            var result = _controller.CreateTag(createDto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdAtResult = result.Result as CreatedAtActionResult;
            createdAtResult!.Value.Should().BeEquivalentTo(createdTag);
            createdAtResult.ActionName.Should().Be(nameof(TagController.GetTagById));
        }

        [Fact]
        public void CreateTag_WhenTagNameIsEmpty_ReturnsBadRequest()
        {
            var createDto = new CreateTagDTO { TagName = "" };

            var result = _controller.CreateTag(createDto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Tag name is required" });
        }

        [Fact]
        public void CreateTag_WhenTagNameIsWhitespace_ReturnsBadRequest()
        {
            var createDto = new CreateTagDTO { TagName = "   " };

            var result = _controller.CreateTag(createDto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Tag name is required" });
        }

        [Fact]
        public void CreateTag_WhenTagAlreadyExists_ReturnsConflict()
        {
            var createDto = new CreateTagDTO { TagName = "ExistingTag" };
            var existingTag = new TagDTO { TagId = 1, TagName = "ExistingTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("ExistingTag")).Returns(existingTag);

            var result = _controller.CreateTag(createDto);

            result.Result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.Result as ConflictObjectResult;
            conflictResult!.Value.Should().BeEquivalentTo(new { message = "Tag already exists" });
            _tagServiceMock.Verify(s => s.CreateTag(It.IsAny<CreateTagDTO>()), Times.Never);
        }

        [Fact]
        public void CreateTag_WhenException_ReturnsStatusCode500()
        {
            var createDto = new CreateTagDTO { TagName = "NewTag" };
            _tagServiceMock.Setup(s => s.GetTagByName("NewTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.CreateTag(createDto)).Throws(new Exception("Database error"));

            var result = _controller.CreateTag(createDto);

            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        // ============ UPDATE TAG ============

        [Fact]
        public void UpdateTag_WhenValid_ReturnsOkWithUpdatedTag()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };
            var updatedTag = new TagDTO { TagId = 1, TagName = "UpdatedTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("UpdatedTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.UpdateTag(1, updateDto)).Returns(updatedTag);

            var result = _controller.UpdateTag(1, updateDto);

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(updatedTag);
        }

        [Fact]
        public void UpdateTag_WhenTagNameIsEmpty_ReturnsBadRequest()
        {
            var updateDto = new UpdateTagDTO { TagName = "" };

            var result = _controller.UpdateTag(1, updateDto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result.Result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Tag name is required" });
        }

        [Fact]
        public void UpdateTag_WhenTagNameExistsForAnotherTag_ReturnsConflict()
        {
            var updateDto = new UpdateTagDTO { TagName = "ExistingTag" };
            var existingTag = new TagDTO { TagId = 2, TagName = "ExistingTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("ExistingTag")).Returns(existingTag);

            var result = _controller.UpdateTag(1, updateDto);

            result.Result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result.Result as ConflictObjectResult;
            conflictResult!.Value.Should().BeEquivalentTo(new { message = "Tag name already exists" });
            _tagServiceMock.Verify(s => s.UpdateTag(It.IsAny<int>(), It.IsAny<UpdateTagDTO>()), Times.Never);
        }

        [Fact]
        public void UpdateTag_WhenTagNameExistsForSameTag_ReturnsOk()
        {
            var updateDto = new UpdateTagDTO { TagName = "SameTag" };
            var sameTag = new TagDTO { TagId = 1, TagName = "SameTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };
            var updatedTag = new TagDTO { TagId = 1, TagName = "SameTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("SameTag")).Returns(sameTag);
            _tagServiceMock.Setup(s => s.UpdateTag(1, updateDto)).Returns(updatedTag);

            var result = _controller.UpdateTag(1, updateDto);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void UpdateTag_WhenTagNotFound_ReturnsNotFound()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };
            _tagServiceMock.Setup(s => s.GetTagByName("UpdatedTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.UpdateTag(999, updateDto)).Returns((TagDTO?)null);

            var result = _controller.UpdateTag(999, updateDto);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result.Result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Tag not found" });
        }

        [Fact]
        public void UpdateTag_WhenException_ReturnsStatusCode500()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };
            _tagServiceMock.Setup(s => s.GetTagByName("UpdatedTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.UpdateTag(1, updateDto)).Throws(new Exception("Database error"));

            var result = _controller.UpdateTag(1, updateDto);

            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        // ============ DELETE TAG ============

        [Fact]
        public void DeleteTag_WhenSuccessful_ReturnsOk()
        {
            _tagServiceMock.Setup(s => s.DeleteTag(1)).Returns(true);

            var result = _controller.DeleteTag(1);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { message = "Tag deleted successfully" });
        }

        [Fact]
        public void DeleteTag_WhenTagNotFound_ReturnsNotFound()
        {
            _tagServiceMock.Setup(s => s.DeleteTag(999)).Returns(false);

            var result = _controller.DeleteTag(999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().BeEquivalentTo(new { message = "Tag not found" });
        }

        [Fact]
        public void DeleteTag_WhenException_ReturnsStatusCode500()
        {
            _tagServiceMock.Setup(s => s.DeleteTag(1)).Throws(new Exception("Database error"));

            var result = _controller.DeleteTag(1);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        // ============ SEARCH TAGS ============

        [Fact]
        public void SearchTags_WhenQueryProvided_ReturnsOkWithMatchingTags()
        {
            var tags = new List<TagDTO>
            {
                new TagDTO { TagId = 1, TagName = "General", CreatedAt = DateTime.UtcNow, PostCount = 5 },
                new TagDTO { TagId = 2, TagName = "General Knowledge", CreatedAt = DateTime.UtcNow, PostCount = 3 }
            };
            _tagServiceMock.Setup(s => s.SearchTags("general")).Returns(tags);

            var result = _controller.SearchTags("general");

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public void SearchTags_WhenQueryIsEmpty_ReturnsOkWithEmptyList()
        {
            var result = _controller.SearchTags("");

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedTags = okResult!.Value as List<TagDTO>;
            returnedTags.Should().BeEmpty();
            _tagServiceMock.Verify(s => s.SearchTags(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SearchTags_WhenQueryIsWhitespace_ReturnsOkWithEmptyList()
        {
            var result = _controller.SearchTags("   ");

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedTags = okResult!.Value as List<TagDTO>;
            returnedTags.Should().BeEmpty();
            _tagServiceMock.Verify(s => s.SearchTags(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void SearchTags_WhenNoMatches_ReturnsOkWithEmptyList()
        {
            var tags = new List<TagDTO>();
            _tagServiceMock.Setup(s => s.SearchTags("nonexistent")).Returns(tags);

            var result = _controller.SearchTags("nonexistent");

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedTags = okResult!.Value as List<TagDTO>;
            returnedTags.Should().BeEmpty();
        }

        [Fact]
        public void SearchTags_WhenException_ReturnsStatusCode500()
        {
            _tagServiceMock.Setup(s => s.SearchTags("test")).Throws(new Exception("Database error"));

            var result = _controller.SearchTags("test");

            result.Result.Should().BeOfType<ObjectResult>();
            var objectResult = result.Result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
        }

        // ============ VERIFICATION TESTS ============

        [Fact]
        public void GetAllTags_VerifiesServiceCalledOnce()
        {
            var tags = new List<TagDTO>();
            _tagServiceMock.Setup(s => s.GetAllTags()).Returns(tags);

            _controller.GetAllTags();

            _tagServiceMock.Verify(s => s.GetAllTags(), Times.Once);
        }

        [Fact]
        public void GetTagById_VerifiesServiceCalledOnce()
        {
            var tag = new TagDTO { TagId = 1, TagName = "Test", CreatedAt = DateTime.UtcNow, PostCount = 0 };
            _tagServiceMock.Setup(s => s.GetTagById(1)).Returns(tag);

            _controller.GetTagById(1);

            _tagServiceMock.Verify(s => s.GetTagById(1), Times.Once);
        }

        [Fact]
        public void CreateTag_VerifiesServiceMethodsCalled()
        {
            var createDto = new CreateTagDTO { TagName = "NewTag" };
            var createdTag = new TagDTO { TagId = 1, TagName = "NewTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("NewTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.CreateTag(createDto)).Returns(createdTag);

            _controller.CreateTag(createDto);

            _tagServiceMock.Verify(s => s.GetTagByName("NewTag"), Times.Once);
            _tagServiceMock.Verify(s => s.CreateTag(createDto), Times.Once);
        }

        [Fact]
        public void UpdateTag_VerifiesServiceMethodsCalled()
        {
            var updateDto = new UpdateTagDTO { TagName = "UpdatedTag" };
            var updatedTag = new TagDTO { TagId = 1, TagName = "UpdatedTag", CreatedAt = DateTime.UtcNow, PostCount = 0 };

            _tagServiceMock.Setup(s => s.GetTagByName("UpdatedTag")).Returns((TagDTO?)null);
            _tagServiceMock.Setup(s => s.UpdateTag(1, updateDto)).Returns(updatedTag);

            _controller.UpdateTag(1, updateDto);

            _tagServiceMock.Verify(s => s.GetTagByName("UpdatedTag"), Times.Once);
            _tagServiceMock.Verify(s => s.UpdateTag(1, updateDto), Times.Once);
        }

        [Fact]
        public void DeleteTag_VerifiesServiceCalledOnce()
        {
            _tagServiceMock.Setup(s => s.DeleteTag(1)).Returns(true);

            _controller.DeleteTag(1);

            _tagServiceMock.Verify(s => s.DeleteTag(1), Times.Once);
        }

        [Fact]
        public void SearchTags_VerifiesServiceCalledOnce()
        {
            var tags = new List<TagDTO>();
            _tagServiceMock.Setup(s => s.SearchTags("test")).Returns(tags);

            _controller.SearchTags("test");

            _tagServiceMock.Verify(s => s.SearchTags("test"), Times.Once);
        }
    }
}


