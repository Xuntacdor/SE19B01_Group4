using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class WordServiceTests
    {
        private readonly Mock<IWordRepository> _repoMock;
        private readonly WordService _service;

        public WordServiceTests()
        {
            _repoMock = new Mock<IWordRepository>();
            _service = new WordService(_repoMock.Object);
        }

        private static List<Word> FakeWords() => new()
        {
            new Word { WordId = 1, Term = "a" },
            new Word { WordId = 2, Term = "b" },
            new Word { WordId = 3, Term = "c" }
        };

        [Fact]
        public void GetByIds_ReturnsFiltered()
        {
            _repoMock.Setup(r => r.SearchWords("")).Returns(FakeWords());
            var result = _service.GetByIds(new List<int> { 1, 3 });
            result.Should().HaveCount(2);
            result.Select(w => w.WordId).Should().BeEquivalentTo(new[] { 1, 3 });
        }

        [Fact]
        public void GetById_CallsRepo()
        {
            var w = new Word { WordId = 5 };
            _repoMock.Setup(r => r.GetById(5)).Returns(w);
            _service.GetById(5).Should().Be(w);
        }

        [Fact]
        public void GetByName_CallsRepo()
        {
            var w = new Word { Term = "a" };
            _repoMock.Setup(r => r.GetByName("a")).Returns(w);
            _service.GetByName("a").Should().Be(w);
        }

        [Fact]
        public void Add_CallsRepo()
        {
            var w = new Word();
            _service.Add(w);
            _repoMock.Verify(r => r.Add(w), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Update_CallsRepo()
        {
            var w = new Word();
            _service.Update(w);
            _repoMock.Verify(r => r.Update(w), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_DeletesWhenFound()
        {
            var w = new Word { WordId = 1 };
            _repoMock.Setup(r => r.GetById(1)).Returns(w);
            _service.Delete(1);
            _repoMock.Verify(r => r.Delete(w), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_DoesNothingWhenNull()
        {
            _repoMock.Setup(r => r.GetById(1)).Returns((Word)null);
            _service.Delete(1);
            _repoMock.Verify(r => r.Delete(It.IsAny<Word>()), Times.Never);
        }

        [Fact]
        public void Save_CallsRepo()
        {
            _service.Save();
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Search_CallsRepo()
        {
            var list = FakeWords();
            _repoMock.Setup(r => r.SearchWords("x")).Returns(list);
            _service.Search("x").Should().BeEquivalentTo(list);
        }

        [Fact]
        public void AddWordToGroup_CallsRepo()
        {
            _service.AddWordToGroup(1, 2);
            _repoMock.Verify(r => r.AddWordToGroup(1, 2), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void RemoveWordFromGroup_CallsRepo()
        {
            _service.RemoveWordFromGroup(1, 2);
            _repoMock.Verify(r => r.RemoveWordFromGroup(1, 2), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void GetWordsByGroup_CallsRepo()
        {
            var list = FakeWords();
            _repoMock.Setup(r => r.GetWordsByGroup(3)).Returns(list);
            _service.GetWordsByGroup(3).Should().BeEquivalentTo(list);
        }

        [Fact]
        public void IsWordInGroup_ReturnsRepoValue()
        {
            _repoMock.Setup(r => r.IsWordInGroup(3, 5)).Returns(true);
            _service.IsWordInGroup(3, 5).Should().BeTrue();
        }

        [Fact]
        public void LookupOrFetch_CallsRepo()
        {
            var w = new Word { Term = "a" };
            _repoMock.Setup(r => r.LookupOrFetch("a")).Returns(w);
            _service.LookupOrFetch("a").Should().Be(w);
        }
    }
}
