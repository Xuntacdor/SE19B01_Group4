using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class TagControllerTests
    {
        private readonly Mock<ITagService> _tagService;
        private readonly TagController _controller;

        public TagControllerTests()
        {
            _tagService = new Mock<ITagService>();
            _controller = new TagController(_tagService.Object);
        }

        // ============ GET ALL TAGS ============

        [Fact]
        public async Task GetAllTags_ReturnsOk()
        {
            var tags = new List<TagDTO>
            {
                new TagDTO { TagId = 1, TagName = "IELTS" },
                new TagDTO { TagId = 2, TagName = "Speaking" }
            };
            _tagService.Setup(s => s.GetAllTagsAsync()).ReturnsAsync(tags);

            var result = await _controller.GetAllTags();

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public async Task GetAllTags_WhenException_ReturnsServerError()
        {
            _tagService.Setup(s => s.GetAllTagsAsync()).ThrowsAsync(new Exception("Database error"));

            var result = await _controller.GetAllTags();

            result.Result.Should().BeOfType<ObjectResult>();
            var objResult = result.Result as ObjectResult;
            objResult!.StatusCode.Should().Be(500);
        }

        // ============ GET TAG BY ID ============

        [Fact]
        public async Task GetTagById_WhenFound_ReturnsOk()
        {
            var tag = new TagDTO { TagId = 1, TagName = "IELTS" };
            _tagService.Setup(s => s.GetTagByIdAsync(1)).ReturnsAsync(tag);

            var result = await _controller.GetTagById(1);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetTagById_WhenNotFound_ReturnsNotFound()
        {
            _tagService.Setup(s => s.GetTagByIdAsync(999)).ReturnsAsync((TagDTO?)null);

            var result = await _controller.GetTagById(999);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result.Result as NotFoundObjectResult;
            notFound!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetTagById_WhenException_ReturnsServerError()
        {
            _tagService.Setup(s => s.GetTagByIdAsync(1)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.GetTagById(1);

            result.Result.Should().BeOfType<ObjectResult>();
            var objResult = result.Result as ObjectResult;
            objResult!.StatusCode.Should().Be(500);
        }

        // ============ CREATE TAG ============

        [Fact]
        public async Task CreateTag_WhenTagNameEmpty_ReturnsBadRequest()
        {
            var dto = new CreateTagDTO { TagName = "" };

            var result = await _controller.CreateTag(dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = result.Result as BadRequestObjectResult;
            badRequest!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTag_WhenDuplicateTag_ReturnsConflict()
        {
            var dto = new CreateTagDTO { TagName = "IELTS" };
            var existingTag = new TagDTO { TagId = 1, TagName = "IELTS" };
            _tagService.Setup(s => s.GetTagByNameAsync("IELTS")).ReturnsAsync(existingTag);

            var result = await _controller.CreateTag(dto);

            result.Result.Should().BeOfType<ConflictObjectResult>();
            var conflict = result.Result as ConflictObjectResult;
            conflict!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTag_WhenSuccessful_ReturnsCreated()
        {
            var dto = new CreateTagDTO { TagName = "NewTag" };
            var created = new TagDTO { TagId = 1, TagName = "NewTag" };
            _tagService.Setup(s => s.GetTagByNameAsync("NewTag")).ReturnsAsync((TagDTO?)null);
            _tagService.Setup(s => s.CreateTagAsync(dto)).ReturnsAsync(created);

            var result = await _controller.CreateTag(dto);

            result.Result.Should().BeOfType<CreatedAtActionResult>();
            var createdAt = result.Result as CreatedAtActionResult;
            createdAt!.ActionName.Should().Be(nameof(TagController.GetTagById));
            createdAt.RouteValues!["id"].Should().Be(1);
        }

        [Fact]
        public async Task CreateTag_WhenException_ReturnsServerError()
        {
            var dto = new CreateTagDTO { TagName = "Tag" };
            _tagService.Setup(s => s.GetTagByNameAsync("Tag")).ReturnsAsync((TagDTO?)null);
            _tagService.Setup(s => s.CreateTagAsync(dto)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.CreateTag(dto);

            result.Result.Should().BeOfType<ObjectResult>();
            var objResult = result.Result as ObjectResult;
            objResult!.StatusCode.Should().Be(500);
        }

        // ============ UPDATE TAG ============

        [Fact]
        public async Task UpdateTag_WhenTagNameEmpty_ReturnsBadRequest()
        {
            var dto = new UpdateTagDTO { TagName = "" };

            var result = await _controller.UpdateTag(1, dto);

            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTag_WhenDuplicateName_ReturnsConflict()
        {
            var dto = new UpdateTagDTO { TagName = "Existing" };
            var existingTag = new TagDTO { TagId = 2, TagName = "Existing" };
            _tagService.Setup(s => s.GetTagByNameAsync("Existing")).ReturnsAsync(existingTag);

            var result = await _controller.UpdateTag(1, dto);

            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task UpdateTag_WhenSuccessful_ReturnsOk()
        {
            var dto = new UpdateTagDTO { TagName = "Updated" };
            var updated = new TagDTO { TagId = 1, TagName = "Updated" };
            _tagService.Setup(s => s.GetTagByNameAsync("Updated")).ReturnsAsync((TagDTO?)null);
            _tagService.Setup(s => s.UpdateTagAsync(1, dto)).ReturnsAsync(updated);

            var result = await _controller.UpdateTag(1, dto);

            result.Result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateTag_WhenNotFound_ReturnsNotFound()
        {
            var dto = new UpdateTagDTO { TagName = "Updated" };
            _tagService.Setup(s => s.GetTagByNameAsync("Updated")).ReturnsAsync((TagDTO?)null);
            _tagService.Setup(s => s.UpdateTagAsync(999, dto)).ReturnsAsync((TagDTO?)null);

            var result = await _controller.UpdateTag(999, dto);

            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateTag_WhenException_ReturnsServerError()
        {
            var dto = new UpdateTagDTO { TagName = "Updated" };
            _tagService.Setup(s => s.GetTagByNameAsync("Updated")).ReturnsAsync((TagDTO?)null);
            _tagService.Setup(s => s.UpdateTagAsync(1, dto)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.UpdateTag(1, dto);

            result.Result.Should().BeOfType<ObjectResult>();
            var objResult = result.Result as ObjectResult;
            objResult!.StatusCode.Should().Be(500);
        }

        // ============ DELETE TAG ============

        [Fact]
        public async Task DeleteTag_WhenSuccessful_ReturnsOk()
        {
            _tagService.Setup(s => s.DeleteTagAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteTag(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteTag_WhenNotFound_ReturnsNotFound()
        {
            _tagService.Setup(s => s.DeleteTagAsync(999)).ReturnsAsync(false);

            var result = await _controller.DeleteTag(999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteTag_WhenException_ReturnsServerError()
        {
            _tagService.Setup(s => s.DeleteTagAsync(1)).ThrowsAsync(new Exception("Error"));

            var result = await _controller.DeleteTag(1);

            result.Should().BeOfType<ObjectResult>();
            var objResult = result as ObjectResult;
            objResult!.StatusCode.Should().Be(500);
        }

        // ============ SEARCH TAGS ============

        [Fact]
        public async Task SearchTags_WhenQueryEmpty_ReturnsEmptyList()
        {
            var result = await _controller.SearchTags("");

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeAssignableTo<IEnumerable<TagDTO>>();
        }

        [Fact]
        public async Task SearchTags_WhenValidQuery_ReturnsTags()
        {
            var tags = new List<TagDTO> { new TagDTO { TagId = 1, TagName = "IELTS" } };
            _tagService.Setup(s => s.SearchTagsAsync("IELTS")).ReturnsAsync(tags);

            var result = await _controller.SearchTags("IELTS");

            result.Result.Should().BeOfType<OkObjectResult>();
            var ok = result.Result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(tags);
        }

        [Fact]
        public async Task SearchTags_WhenException_ReturnsServerError()
        {
            _tagService.Setup(s => s.SearchTagsAsync("test")).ThrowsAsync(new Exception("Error"));

            var result = await _controller.SearchTags("test");

            result.Result.Should().BeOfType<ObjectResult>();
            var objResult = result.Result as ObjectResult;
            objResult!.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task SearchTags_WhenQueryWhitespace_ReturnsEmptyList()
        {
            var result = await _controller.SearchTags("   ");

            result.Result.Should().BeOfType<OkObjectResult>();
        }
    }
}






