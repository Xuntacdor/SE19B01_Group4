using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controllers
{
    public class VocabGroupsControllerTests
    {
        private readonly Mock<IVocabGroupService> _groupServiceMock;
        private readonly Mock<IWordService> _wordServiceMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly VocabGroupsController _controller;

        public VocabGroupsControllerTests()
        {
            _groupServiceMock = new Mock<IVocabGroupService>();
            _wordServiceMock = new Mock<IWordService>();
            _userServiceMock = new Mock<IUserService>();

            _controller = new VocabGroupsController(
                _groupServiceMock.Object,
                _wordServiceMock.Object,
                _userServiceMock.Object
            );
        }

        private static VocabGroup CreateGroup(int id = 1, int userId = 1, string name = "Group A") =>
            new VocabGroup
            {
                GroupId = id,
                Groupname = name,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Words = new List<Word>
                {
                    new Word { WordId = 1, Term = "test", Meaning = "meaning", Example = "example", Audio = "audio.mp3" }
                }
            };

        private static VocabGroupDto CreateDto(int id = 1, int userId = 1, string name = "Group A") =>
            new VocabGroupDto
            {
                GroupId = id,
                Groupname = name,
                UserId = userId,
                WordIds = new List<int> { 1 }
            };

        // ---------- GET BY ID ----------

        [Fact]
        public void GetById_ReturnsOk_WhenFound()
        {
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);

            var actionResult = _controller.GetById(1);
            var result = actionResult.Result as OkObjectResult;

            result.Should().NotBeNull();
            var dto = result.Value as VocabGroupDto;
            dto.Should().NotBeNull();
            dto.GroupId.Should().Be(group.GroupId);
        }

        [Fact]
        public void GetById_ReturnsNotFound_WhenNull()
        {
            _groupServiceMock.Setup(x => x.GetById(1)).Returns((VocabGroup)null);

            var actionResult = _controller.GetById(1);
            actionResult.Result.Should().BeOfType<NotFoundResult>();
        }

        // ---------- GET BY USER ----------

        [Fact]
        public void GetByUser_ReturnsAll_WhenNameIsNull()
        {
            var groups = new List<VocabGroup> { CreateGroup(1), CreateGroup(2, 1, "Another") };
            _groupServiceMock.Setup(x => x.GetByUser(1)).Returns(groups);

            var actionResult = _controller.GetByUser(1, null);
            var result = actionResult.Result as OkObjectResult;

            result.Should().NotBeNull();
            var dtoList = result.Value as IEnumerable<VocabGroupDto>;
            dtoList.Should().HaveCount(2);
        }

        [Fact]
        public void GetByUser_ReturnsByName_WhenNameProvided_AndFound()
        {
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetByName(1, "Group A")).Returns(group);

            var actionResult = _controller.GetByUser(1, "Group A");
            var result = actionResult.Result as OkObjectResult;

            result.Should().NotBeNull();
            (result.Value as VocabGroupDto).Groupname.Should().Be("Group A");
        }

        [Fact]
        public void GetByUser_ReturnsNotFound_WhenNameProvided_NotFound()
        {
            _groupServiceMock.Setup(x => x.GetByName(1, "X")).Returns((VocabGroup)null);

            var actionResult = _controller.GetByUser(1, "X");
            actionResult.Result.Should().BeOfType<NotFoundResult>();
        }

        // ---------- COUNT WORDS ----------

        [Fact]
        public void CountWords_ReturnsCount()
        {
            _groupServiceMock.Setup(x => x.CountWords(1)).Returns(5);

            var actionResult = _controller.CountWords(1);
            var result = actionResult.Result as OkObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().Be(5);
        }

        // ---------- ADD ----------

        [Fact]
        public void Add_ReturnsCreated_WhenValid()
        {
            var dto = CreateDto();
            _userServiceMock.Setup(x => x.Exists(1)).Returns(true);
            _wordServiceMock.Setup(x => x.GetByIds(It.IsAny<List<int>>()))
                .Returns(new List<Word> { new Word { WordId = 1 } });
            _groupServiceMock.Setup(x => x.Add(It.IsAny<VocabGroup>()))
                .Callback<VocabGroup>(g => g.GroupId = 99);

            var result = _controller.Add(dto) as CreatedAtActionResult;
            result.Should().NotBeNull();
            var created = result.Value as VocabGroupDto;
            created.Should().NotBeNull();
            created.GroupId.Should().Be(99);
        }

        [Fact]
        public void Add_ReturnsBadRequest_WhenInvalidUser()
        {
            var dto = CreateDto();
            _userServiceMock.Setup(x => x.Exists(dto.UserId)).Returns(false);

            var result = _controller.Add(dto) as BadRequestObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().Be("Invalid UserId");
        }

        // ---------- UPDATE ----------

        [Fact]
        public void Update_ReturnsNoContent_WhenSuccess()
        {
            var dto = CreateDto();
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);
            _userServiceMock.Setup(x => x.Exists(1)).Returns(true);
            _wordServiceMock.Setup(x => x.GetByIds(It.IsAny<List<int>>()))
                .Returns(group.Words.ToList());

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Update_ReturnsBadRequest_WhenIdMismatch()
        {
            var dto = CreateDto(2);
            var result = _controller.Update(1, dto);

            result.Should().BeOfType<BadRequestResult>();
        }

        [Fact]
        public void Update_ReturnsNotFound_WhenGroupNull()
        {
            var dto = CreateDto();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns((VocabGroup)null);

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Update_ReturnsBadRequest_WhenInvalidUser()
        {
            var dto = CreateDto();
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);
            _userServiceMock.Setup(x => x.Exists(1)).Returns(false);

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ---------- DELETE ----------

        [Fact]
        public void Delete_ReturnsNoContent_WhenSuccess()
        {
            var result = _controller.Delete(1);

            result.Should().BeOfType<NoContentResult>();
            _groupServiceMock.Verify(x => x.Delete(1), Times.Once);
        }

        [Fact]
        public void Delete_ReturnsNotFound_WhenThrowsKeyNotFound()
        {
            _groupServiceMock.Setup(x => x.Delete(1)).Throws(new KeyNotFoundException("Not found"));

            var result = _controller.Delete(1) as NotFoundObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(new { message = "Not found" });
        }

        // ---------- GET WORDS IN GROUP ----------

        [Fact]
        public void GetWordsInGroup_ReturnsWords_WhenFound()
        {
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);

            var actionResult = _controller.GetWordsInGroup(1);
            var result = actionResult.Result as OkObjectResult;

            result.Should().NotBeNull();
            var words = result.Value as IEnumerable<WordDto>;
            words.Should().ContainSingle();
        }

        [Fact]
        public void GetWordsInGroup_ReturnsNotFound_WhenNull()
        {
            _groupServiceMock.Setup(x => x.GetById(1)).Returns((VocabGroup)null);

            var actionResult = _controller.GetWordsInGroup(1);
            actionResult.Result.Should().BeOfType<NotFoundResult>();
        }

        // ---------- ADD WORD TO GROUP ----------

        [Fact]
        public void AddWordToGroup_ReturnsOk_WhenSuccess()
        {
            var group = CreateGroup();
            var word = new Word { WordId = 2, Term = "new" };
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);
            _wordServiceMock.Setup(x => x.GetById(2)).Returns(word);

            var result = _controller.AddWordToGroup(1, 2) as OkObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            var message = result.Value.GetType().GetProperty("message")?.GetValue(result.Value)?.ToString();
            message.Should().NotBeNull();
            message.Should().Contain("added");
        }

        [Fact]
        public void AddWordToGroup_ReturnsNotFound_WhenGroupNull()
        {
            _groupServiceMock.Setup(x => x.GetById(1)).Returns((VocabGroup)null);

            var result = _controller.AddWordToGroup(1, 1) as NotFoundObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            var message = result.Value.GetType().GetProperty("message")?.GetValue(result.Value)?.ToString();
            message.Should().Be("Group not found");
        }

        [Fact]
        public void AddWordToGroup_ReturnsNotFound_WhenWordNull()
        {
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);
            _wordServiceMock.Setup(x => x.GetById(2)).Returns((Word)null);

            var result = _controller.AddWordToGroup(1, 2) as NotFoundObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            var message = result.Value.GetType().GetProperty("message")?.GetValue(result.Value)?.ToString();
            message.Should().Be("Word not found");
        }

        [Fact]
        public void AddWordToGroup_ReturnsBadRequest_WhenAlreadyExists()
        {
            var group = CreateGroup();
            var word = group.Words.First();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);
            _wordServiceMock.Setup(x => x.GetById(1)).Returns(word);

            var result = _controller.AddWordToGroup(1, 1) as BadRequestObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            var message = result.Value.GetType().GetProperty("message")?.GetValue(result.Value)?.ToString();
            message.Should().Be("Word already exists in this group");
        }

        // ---------- REMOVE WORD FROM GROUP ----------

        [Fact]
        public void RemoveWordFromGroup_ReturnsNoContent_WhenSuccess()
        {
            var group = CreateGroup();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);

            var result = _controller.RemoveWordFromGroup(1, 1);

            result.Should().BeOfType<NoContentResult>();
            _groupServiceMock.Verify(x => x.Update(It.IsAny<VocabGroup>()), Times.Once);
        }

        [Fact]
        public void RemoveWordFromGroup_ReturnsNotFound_WhenGroupMissing()
        {
            _groupServiceMock.Setup(x => x.GetById(1)).Returns((VocabGroup)null);

            var result = _controller.RemoveWordFromGroup(1, 1) as NotFoundObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            var message = result.Value.GetType().GetProperty("message")?.GetValue(result.Value)?.ToString();
            message.Should().Be("Group not found");
        }

        [Fact]
        public void RemoveWordFromGroup_ReturnsNotFound_WhenWordMissing()
        {
            var group = CreateGroup();
            group.Words.Clear();
            _groupServiceMock.Setup(x => x.GetById(1)).Returns(group);

            var result = _controller.RemoveWordFromGroup(1, 1) as NotFoundObjectResult;

            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            var message = result.Value.GetType().GetProperty("message")?.GetValue(result.Value)?.ToString();
            message.Should().Be("Word not in this group");
        }
    }
}
