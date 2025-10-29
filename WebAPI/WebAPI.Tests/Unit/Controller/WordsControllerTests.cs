using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using WebAPI.Controllers;
using WebAPI.Services;
using WebAPI.Models;
using WebAPI.DTOs;

namespace WebAPI.Tests.Unit.Controllers
{
    public class WordsControllerTests
    {
        private readonly Mock<IWordService> _serviceMock;
        private readonly WordsController _controller;

        public WordsControllerTests()
        {
            _serviceMock = new Mock<IWordService>();
            _controller = new WordsController(_serviceMock.Object);
        }

        private static Word CreateWord(int id = 1, string term = "distort")
        {
            return new Word
            {
                WordId = id,
                Term = term,
                Meaning = "to twist or change",
                Audio = "audio.mp3",
                Example = "He distorted the truth.",
                Groups = new List<VocabGroup> { new() { GroupId = 10 } }
            };
        }

        // ==== GET BY ID ====

        [Fact]
        public void GetById_WhenWordExists_ReturnsOkWithDto()
        {
            var word = CreateWord();
            _serviceMock.Setup(s => s.GetById(1)).Returns(word);

            var result = _controller.GetById(1);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var dto = ok.Value.Should().BeOfType<WordDto>().Subject;
            dto.WordId.Should().Be(1);
            dto.Term.Should().Be("distort");
        }

        [Fact]
        public void GetById_WhenWordNotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetById(2)).Returns((Word)null);

            var result = _controller.GetById(2);

            result.Should().BeOfType<NotFoundResult>();
        }

        // ==== GET BY TERM ====

        [Fact]
        public void GetByTerm_WhenTermMissing_ReturnsBadRequest()
        {
            var result = _controller.GetByTerm(null);

            var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            bad.Value.Should().Be("Term is required");
        }

        [Fact]
        public void GetByTerm_WhenWordExists_ReturnsOkWithDto()
        {
            var word = CreateWord();
            _serviceMock.Setup(s => s.GetByName("distort")).Returns(word);

            var result = _controller.GetByTerm("distort");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var dto = ok.Value.Should().BeOfType<WordDto>().Subject;
            dto.Term.Should().Be("distort");
        }

        [Fact]
        public void GetByTerm_WhenWordNotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetByName("x")).Returns((Word)null);

            var result = _controller.GetByTerm("x");

            result.Should().BeOfType<NotFoundResult>();
        }

        // ==== SEARCH ====

        [Fact]
        public void Search_WhenCalled_ReturnsOkWithDtos()
        {
            var list = new List<Word> { CreateWord(1), CreateWord(2, "expand") };
            _serviceMock.Setup(s => s.Search("ex")).Returns(list);

            var result = _controller.Search("ex");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var values = ok.Value.Should().BeAssignableTo<IEnumerable<WordDto>>().Subject.ToList();
            values.Should().HaveCount(2);
            values[0].Term.Should().Be("distort");
        }

        // ==== ADD ====

        [Fact]
        public void Add_WhenCalled_ReturnsCreatedAtActionWithDto()
        {
            var dto = new WordDto
            {
                Term = "run",
                Meaning = "to move fast",
                Audio = "run.mp3",
                Example = "He can run fast",
                GroupIds = new List<int> { 1, 2 }
            };

            _serviceMock.Setup(s => s.Add(It.IsAny<Word>()))
                        .Callback<Word>(w => w.WordId = 99);

            var result = _controller.Add(dto);

            var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(WordsController.GetById));
            var returnDto = created.Value.Should().BeOfType<WordDto>().Subject;
            returnDto.WordId.Should().Be(99);
            returnDto.Term.Should().Be("run");
            _serviceMock.Verify(s => s.Add(It.IsAny<Word>()), Times.Once);
        }

        // ==== UPDATE ====

        [Fact]
        public void Update_WhenWordExists_ReturnsNoContent()
        {
            var existing = CreateWord(1);
            var dto = new WordDto
            {
                Term = "update",
                Meaning = "to change",
                Audio = "new.mp3",
                Example = "He updated the record.",
                GroupIds = new List<int> { 5 }
            };
            _serviceMock.Setup(s => s.GetById(1)).Returns(existing);

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<NoContentResult>();
            existing.Term.Should().Be("update");
            existing.Groups.Should().ContainSingle(g => g.GroupId == 5);
            _serviceMock.Verify(s => s.Update(It.Is<Word>(w => w.Term == "update")), Times.Once);
        }

        [Fact]
        public void Update_WhenWordNotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetById(1)).Returns((Word)null);

            var dto = new WordDto
            {
                Term = "new",
                Meaning = "desc",
                GroupIds = new List<int>()
            };

            var result = _controller.Update(1, dto);

            result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.Update(It.IsAny<Word>()), Times.Never);
        }

        // ==== DELETE ====

        [Fact]
        public void Delete_WhenWordExists_ReturnsNoContent()
        {
            var word = CreateWord();
            _serviceMock.Setup(s => s.GetById(1)).Returns(word);

            var result = _controller.Delete(1);

            result.Should().BeOfType<NoContentResult>();
            _serviceMock.Verify(s => s.Delete(1), Times.Once);
        }

        [Fact]
        public void Delete_WhenWordNotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetById(1)).Returns((Word)null);

            var result = _controller.Delete(1);

            result.Should().BeOfType<NotFoundResult>();
            _serviceMock.Verify(s => s.Delete(It.IsAny<int>()), Times.Never);
        }

        // ==== LOOKUP ====

        [Fact]
        public void Lookup_WhenWordExists_ReturnsOkWithDto()
        {
            var word = CreateWord();
            _serviceMock.Setup(s => s.LookupOrFetch("distort")).Returns(word);

            var result = _controller.Lookup("distort");

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var dto = ok.Value.Should().BeOfType<WordDto>().Subject;
            dto.Term.Should().Be("distort");
        }

        [Fact]
        public void Lookup_WhenWordNotFound_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.LookupOrFetch("ghost")).Returns((Word)null);

            var result = _controller.Lookup("ghost");

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
