using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class VipPlanServiceTests
    {
        private readonly Mock<IVipPlanRepository> _repo;
        private readonly VipPlanService _service;

        public VipPlanServiceTests()
        {
            _repo = new Mock<IVipPlanRepository>();
            _service = new VipPlanService(_repo.Object);
        }

        [Fact]
        public void GetAll_MapsEntitiesToDto()
        {
            _repo.Setup(r => r.GetAll()).Returns(new List<VipPlan>
            {
                new VipPlan{ VipPlanId=1, PlanName="A", Price=10, DurationDays=30, Description="desc", CreatedAt=DateTime.UtcNow }
            });

            var result = _service.GetAll().ToList();

            result.Should().HaveCount(1);
            result[0].PlanName.Should().Be("A");
        }

        [Fact]
        public void GetById_WhenNull_ReturnsNull()
        {
            _repo.Setup(r => r.GetById(9)).Returns((VipPlan?)null);

            var result = _service.GetById(9);

            result.Should().BeNull();
        }

        [Fact]
        public void Create_ValidInput_PersistsAndReturnsDto()
        {
            var now = DateTime.UtcNow;
            _repo.Setup(r => r.Add(It.IsAny<VipPlan>()));
            _repo.Setup(r => r.SaveChanges());

            var dto = new VipPlanDTO { PlanName = "B", Price = 20, DurationDays = 60, Description = "d" };
            var created = _service.Create(dto);

            created.PlanName.Should().Be("B");
            _repo.Verify(r => r.Add(It.Is<VipPlan>(p => p.PlanName == "B")), Times.Once);
            _repo.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Create_EmptyName_Throws()
        {
            var dto = new VipPlanDTO { PlanName = "  " };
            var act = () => _service.Create(dto);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetById(1)).Returns((VipPlan?)null);

            var result = _service.Update(1, new VipPlanDTO { PlanName = "C" });

            result.Should().BeNull();
        }

        [Fact]
        public void Update_WhenFound_UpdatesAndReturnsDto()
        {
            var entity = new VipPlan { VipPlanId = 2, PlanName = "Old", Price = 10, DurationDays = 30 };
            _repo.Setup(r => r.GetById(2)).Returns(entity);

            var result = _service.Update(2, new VipPlanDTO { PlanName = "New", Price = 25, DurationDays = 45, Description = "x" });

            result!.PlanName.Should().Be("New");
            _repo.Verify(r => r.Update(entity), Times.Once);
            _repo.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsFalse()
        {
            _repo.Setup(r => r.GetById(3)).Returns((VipPlan?)null);

            var ok = _service.Delete(3);

            ok.Should().BeFalse();
        }

        [Fact]
        public void Delete_WhenFound_DeletesAndReturnsTrue()
        {
            var entity = new VipPlan { VipPlanId = 4 };
            _repo.Setup(r => r.GetById(4)).Returns(entity);

            var ok = _service.Delete(4);

            ok.Should().BeTrue();
            _repo.Verify(r => r.Delete(entity), Times.Once);
            _repo.Verify(r => r.SaveChanges(), Times.Once);
        }
    }
}



