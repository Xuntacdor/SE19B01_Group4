using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class TransactionRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetAll_ReturnsAllTransactions()
        {
            using var context = CreateInMemoryContext();
            context.Transactions.Add(TestUtilities.CreateValidTransaction(1, 1));
            context.Transactions.Add(TestUtilities.CreateValidTransaction(2, 2));
            context.SaveChanges();

            var repo = new TransactionRepository(context);

            var result = repo.GetAll().ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.TransactionId == 1);
            Assert.Contains(result, t => t.TransactionId == 2);
        }

        [Fact]
        public void GetAll_WhenNoTransactions_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var repo = new TransactionRepository(context);

            var result = repo.GetAll().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsTransaction()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "user", "user@example.com");
            context.User.Add(user);
            var transaction = TestUtilities.CreateValidTransaction(1, 1);
            context.Transactions.Add(transaction);
            context.SaveChanges();

            var repo = new TransactionRepository(context);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.TransactionId);
            Assert.Equal(100.0m, result.Amount);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new TransactionRepository(context);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetByReference_WhenExists_ReturnsTransaction()
        {
            using var context = CreateInMemoryContext();
            var transaction = TestUtilities.CreateValidTransaction(1, 1);
            transaction.ProviderTxnId = "REF123";
            context.Transactions.Add(transaction);
            context.SaveChanges();

            var repo = new TransactionRepository(context);

            var result = repo.GetByReference("REF123");

            Assert.NotNull(result);
            Assert.Equal("REF123", result.ProviderTxnId);
        }

        [Fact]
        public void GetByReference_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repo = new TransactionRepository(context);

            var result = repo.GetByReference("NONEXISTENT");

            Assert.Null(result);
        }

        [Fact]
        public void IncludeUserAndPlan_ReturnsTransactionsWithUserAndPlan()
        {
            using var context = CreateInMemoryContext();
            var user = TestUtilities.CreateValidUser(1, "user", "user@example.com");
            var plan = new VipPlan { VipPlanId = 1, PlanName = "Basic Plan", Price = 100.0m, DurationDays = 30 };
            context.User.Add(user);
            context.VipPlans.Add(plan);
            var transaction = TestUtilities.CreateValidTransaction(1, 1, 1);
            context.Transactions.Add(transaction);
            context.SaveChanges();

            var repo = new TransactionRepository(context);

            var result = repo.IncludeUserAndPlan().FirstOrDefault(t => t.TransactionId == 1);

            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotNull(result.Plan);
            Assert.Equal("user@example.com", result.User.Email);
            Assert.Equal("Basic Plan", result.Plan.PlanName);
        }

        [Fact]
        public void IncludeUserAndPlan_WhenNoTransactions_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var repo = new TransactionRepository(context);

            var result = repo.IncludeUserAndPlan().ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void Add_AddsTransaction()
        {
            using var context = CreateInMemoryContext();
            var transaction = TestUtilities.CreateValidTransaction(1, 1);
            var repo = new TransactionRepository(context);

            repo.Add(transaction);
            repo.SaveChanges();

            Assert.True(context.Transactions.Any(t => t.TransactionId == 1));
        }

        [Fact]
        public void Update_UpdatesTransaction()
        {
            using var context = CreateInMemoryContext();
            var transaction = TestUtilities.CreateValidTransaction(1, 1);
            context.Transactions.Add(transaction);
            context.SaveChanges();

            transaction.Status = "COMPLETED";
            var repo = new TransactionRepository(context);

            repo.Update(transaction);
            repo.SaveChanges();

            var updated = context.Transactions.FirstOrDefault(t => t.TransactionId == 1);
            Assert.Equal("COMPLETED", updated.Status);
        }
    }
}
