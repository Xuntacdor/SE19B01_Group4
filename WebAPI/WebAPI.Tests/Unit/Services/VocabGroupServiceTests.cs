using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class VocabGroupServiceTests
    {
        private readonly Mock<IVocabGroupRepository> _repoMock;
        private readonly VocabGroupService _service;

        public VocabGroupServiceTests()
        {
            _repoMock = new Mock<IVocabGroupRepository>();
            _service = new VocabGroupService(_repoMock.Object);
        }

        private static VocabGroup CreateGroup(int id = 1, int userId = 10, string name = "Test")
        {
            return new VocabGroup
            {
                GroupId = id,
                UserId = userId,
                Groupname = name,
                Words = new List<Word>()
            };
        }

        [Fact]
        public void GetById_CallsRepository()
        {
            var g = CreateGroup(1);
            _repoMock.Setup(r => r.GetById(1)).Returns(g);
            var result = _service.GetById(1);
            result.Should().Be(g);
            _repoMock.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void GetByUser_CallsRepository()
        {
            var groups = new List<VocabGroup> { CreateGroup(1), CreateGroup(2) };
            _repoMock.Setup(r => r.GetByUser(5)).Returns(groups);
            var result = _service.GetByUser(5);
            result.Should().BeEquivalentTo(groups);
            _repoMock.Verify(r => r.GetByUser(5), Times.Once);
        }

        [Fact]
        public void GetByName_CallsRepository()
        {
            var g = CreateGroup();
            _repoMock.Setup(r => r.GetByName(1, "MyGroup")).Returns(g);
            var result = _service.GetByName(1, "MyGroup");
            result.Should().Be(g);
            _repoMock.Verify(r => r.GetByName(1, "MyGroup"), Times.Once);
        }

        [Fact]
        public void ExistsForUser_CallsRepository()
        {
            _repoMock.Setup(r => r.ExistsForUser(1, "abc")).Returns(true);
            var result = _service.ExistsForUser(1, "abc");
            result.Should().BeTrue();
            _repoMock.Verify(r => r.ExistsForUser(1, "abc"), Times.Once);
        }

        [Fact]
        public void CountWords_CallsRepository()
        {
            _repoMock.Setup(r => r.CountWords(3)).Returns(15);
            var result = _service.CountWords(3);
            result.Should().Be(15);
            _repoMock.Verify(r => r.CountWords(3), Times.Once);
        }

        [Fact]
        public void Add_AddsGroup_WhenNotExists()
        {
            var g = CreateGroup();
            _repoMock.Setup(r => r.ExistsForUser(g.UserId, g.Groupname)).Returns(false);
            _service.Add(g);
            _repoMock.Verify(r => r.Add(g), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Add_Throws_WhenExists()
        {
            var g = CreateGroup();
            _repoMock.Setup(r => r.ExistsForUser(g.UserId, g.Groupname)).Returns(true);
            Action act = () => _service.Add(g);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Group name already exists for this user.");
            _repoMock.Verify(r => r.Add(It.IsAny<VocabGroup>()), Times.Never);
            _repoMock.Verify(r => r.SaveChanges(), Times.Never);
        }

        [Fact]
        public void Update_CallsRepoUpdate_AndSave()
        {
            var g = CreateGroup();
            _service.Update(g);
            _repoMock.Verify(r => r.Update(g), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_RemovesGroup_WhenFound()
        {
            var g = CreateGroup(5);
            _repoMock.Setup(r => r.GetById(5)).Returns(g);
            _service.Delete(5);
            _repoMock.Verify(r => r.Delete(g), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_Throws_WhenNotFound()
        {
            _repoMock.Setup(r => r.GetById(5)).Returns((VocabGroup)null);
            Action act = () => _service.Delete(5);
            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("Group not found.");
            _repoMock.Verify(r => r.Delete(It.IsAny<VocabGroup>()), Times.Never);
            _repoMock.Verify(r => r.SaveChanges(), Times.Never);
        }
    }
}
