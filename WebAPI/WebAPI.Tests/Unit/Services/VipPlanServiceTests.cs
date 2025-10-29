using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.Tests.Unit.Services;

public class VipPlanServiceTests
{
    private readonly Mock<IVipPlanRepository> _repo = new();

    private VipPlanService CreateService() => new VipPlanService(_repo.Object);

    [Fact]
    public void GetById_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.GetById(10)).Returns((VipPlan?)null);
        var svc = CreateService();
        var result = svc.GetById(10);
        Assert.Null(result);
    }

    [Fact]
    public void GetAll_MapsEntitiesToDto()
    {
        _repo.Setup(r => r.GetAll()).Returns(new List<VipPlan>
        {
            new VipPlan{ VipPlanId=1, PlanName="A", Price=10, DurationDays=30, Description="desc", CreatedAt=DateTime.UtcNow }
        });

        var svc = CreateService();
        var result = svc.GetAll().ToList();

        Assert.Single(result);
        Assert.Equal("A", result[0].PlanName);
    }

    [Fact]
    public void Create_ValidInput_PersistsAndReturnsDto()
    {
        _repo.Setup(r => r.Add(It.IsAny<VipPlan>()));
        _repo.Setup(r => r.SaveChanges());

        var svc = CreateService();
        var dto = new VipPlanDTO { PlanName = "B", Price = 20, DurationDays = 60, Description = "d" };
        var created = svc.Create(dto);

        Assert.Equal("B", created.PlanName);
        _repo.Verify(r => r.Add(It.Is<VipPlan>(p => p.PlanName == "B")), Times.Once);
        _repo.Verify(r => r.SaveChanges(), Times.Once);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var svc = CreateService();
        var dto = new VipPlanDTO { PlanName = "  " };
        Assert.Throws<ArgumentException>(() => svc.Create(dto));
    }

    [Fact]
    public void Delete_WhenNotFound_ReturnsFalse()
    {
        _repo.Setup(r => r.GetById(3)).Returns((VipPlan?)null);
        var svc = CreateService();
        var ok = svc.Delete(3);
        Assert.False(ok);
    }

    [Fact]
    public void Delete_WhenFound_DeletesAndReturnsTrue()
    {
        var entity = new VipPlan { VipPlanId = 4 };
        _repo.Setup(r => r.GetById(4)).Returns(entity);
        var svc = CreateService();
        var ok = svc.Delete(4);
        Assert.True(ok);
        _repo.Verify(r => r.Delete(entity), Times.Once);
        _repo.Verify(r => r.SaveChanges(), Times.Once);
    }

    [Fact]
    public void GetById_ReturnsDto_WhenFound()
    {
        var entity = new VipPlan
        {
            VipPlanId = 2,
            PlanName = "Gold",
            Price = 99.5m,
            DurationDays = 30,
            Description = "desc",
            CreatedAt = DateTime.UtcNow
        };
        _repo.Setup(r => r.GetById(2)).Returns(entity);

        var svc = CreateService();
        var dto = svc.GetById(2);

        Assert.NotNull(dto);
        Assert.Equal(entity.VipPlanId, dto!.VipPlanId);
        Assert.Equal(entity.PlanName, dto.PlanName);
        Assert.Equal(entity.Price, dto.Price);
        Assert.Equal(entity.DurationDays, dto.DurationDays);
        Assert.Equal(entity.Description, dto.Description);
        Assert.Equal(entity.CreatedAt, dto.CreatedAt);
    }

    [Fact]
    public void Update_ReturnsNull_WhenNotFound()
    {
        _repo.Setup(r => r.GetById(5)).Returns((VipPlan?)null);
        var svc = CreateService();
        var result = svc.Update(5, new VipPlanDTO());
        Assert.Null(result);
    }

    [Fact]
    public void Update_KeepExisting_WhenNullOrZeroProvided()
    {
        var existing = new VipPlan
        {
            VipPlanId = 7,
            PlanName = "Basic",
            Price = 10m,
            DurationDays = 7,
            Description = "old",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _repo.Setup(r => r.GetById(7)).Returns(existing);

        var svc = CreateService();
        // Provide nulls and zeros to exercise the 'keep existing' branches
        var updated = svc.Update(7, new VipPlanDTO
        {
            PlanName = null,
            Price = 0m,
            DurationDays = 0,
            Description = null
        });

        Assert.NotNull(updated);
        Assert.Equal("Basic", updated!.PlanName);
        Assert.Equal(10m, updated.Price);
        Assert.Equal(7, updated.DurationDays);
        Assert.Equal("old", updated.Description);

        _repo.Verify(r => r.Update(It.Is<VipPlan>(p =>
            p.PlanName == "Basic" &&
            p.Price == 10m &&
            p.DurationDays == 7 &&
            p.Description == "old")), Times.Once);
        _repo.Verify(r => r.SaveChanges(), Times.Once);
    }

    [Fact]
    public void Update_ApplyAllFields_WhenProvided()
    {
        var existing = new VipPlan
        {
            VipPlanId = 3,
            PlanName = "Basic",
            Price = 10m,
            DurationDays = 7,
            Description = "old",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _repo.Setup(r => r.GetById(3)).Returns(existing);

        var svc = CreateService();
        var updated = svc.Update(3, new VipPlanDTO
        {
            PlanName = "Pro",
            Price = 25.5m,
            DurationDays = 14,
            Description = "new"
        });

        Assert.NotNull(updated);
        Assert.Equal("Pro", updated!.PlanName);
        Assert.Equal(25.5m, updated.Price);
        Assert.Equal(14, updated.DurationDays);
        Assert.Equal("new", updated.Description);

        _repo.Verify(r => r.Update(It.Is<VipPlan>(p =>
            p.PlanName == "Pro" &&
            p.Price == 25.5m &&
            p.DurationDays == 14 &&
            p.Description == "new")), Times.Once);
        _repo.Verify(r => r.SaveChanges(), Times.Once);
    }
}
