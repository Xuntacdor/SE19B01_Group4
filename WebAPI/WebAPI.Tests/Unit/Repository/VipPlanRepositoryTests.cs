using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class VipPlanRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetAll_ReturnsAllVipPlansOrderedByPrice()
        {
            using var context = CreateInMemoryContext();
            context.VipPlans.Add(new VipPlan { VipPlanId = 2, PlanName = "Premium", Price = 200.0m, DurationDays = 90 });
            context.VipPlans.Add(new VipPlan { VipPlanId = 1, PlanName = "Basic", Price = 100.0m, DurationDays = 30 });
            context.VipPlans.Add(new VipPlan { VipPlanId = 3, PlanName = "Standard", Price = 150.0m, DurationDays = 60 });
            context.SaveChanges();

            var repo = new VipPlanRepository(context);

            var result = repo.GetAll().ToList();

            Assert.Equal(3, result.Count);
            Assert.Equal("Basic", result[0].PlanName); // Price 100
            Assert.Equal("Standard", result[1].PlanName); // Price 150
            Assert.Equal("Premium", result[2].PlanName); // Price 200
        }

        [Fact]
        public void GetAll_WhenNoVipPlans_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var repo = new VipPlanRepository(context);

            var result = repo.GetAll().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsVipPlan()
        {
            using var context = CreateInMemoryContext();
            var plan = new VipPlan { VipPlanId = 1, PlanName = "Basic", Price = 100.0m, DurationDays = 30 };
            context.VipPlans.Add(plan);
            context.SaveChanges();

            var repo = new VipPlanRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal("Basic", result.PlanName);
            Assert.Equal(100.0m, result.Price);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new VipPlanRepository(context);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void Add_AddsVipPlan()
        {
            using var context = CreateInMemoryContext();
            var plan = new VipPlan { VipPlanId = 1, PlanName = "New Plan", Price = 50.0m, DurationDays = 30 };
            var repo = new VipPlanRepository(context);

            repo.Add(plan);
            repo.SaveChanges();

            Assert.True(context.VipPlans.Any(p => p.PlanName == "New Plan"));
        }

        [Fact]
        public void Update_UpdatesVipPlan()
        {
            using var context = CreateInMemoryContext();
            var plan = new VipPlan { VipPlanId = 1, PlanName = "Old Plan", Price = 100.0m, DurationDays = 30 };
            context.VipPlans.Add(plan);
            context.SaveChanges();

            plan.PlanName = "Updated Plan";
            plan.Price = 150.0m;
            var repo = new VipPlanRepository(context);

            repo.Update(plan);
            repo.SaveChanges();

            var updated = context.VipPlans.FirstOrDefault(p => p.VipPlanId == 1);
            Assert.Equal("Updated Plan", updated.PlanName);
            Assert.Equal(150.0m, updated.Price);
        }

        [Fact]
        public void Delete_DeletesVipPlan()
        {
            using var context = CreateInMemoryContext();
            var plan = new VipPlan { VipPlanId = 1, PlanName = "Plan to Delete", Price = 100.0m, DurationDays = 30 };
            context.VipPlans.Add(plan);
            context.SaveChanges();

            var repo = new VipPlanRepository(context);

            repo.Delete(plan);
            repo.SaveChanges();

            Assert.False(context.VipPlans.Any());
        }
    }
}
