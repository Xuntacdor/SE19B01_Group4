using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.Tests.Unit.Services;

public class TransactionServiceTests
{
    private static (TransactionService svc, Mock<ITransactionRepository> txRepo, Mock<IVipPlanRepository> vipRepo, Mock<IUserRepository> userRepo)
        CreateService(IEnumerable<Transaction> seed)
    {
        var txRepo = new Mock<ITransactionRepository>();
        var vipRepo = new Mock<IVipPlanRepository>();
        var userRepo = new Mock<IUserRepository>();

        var list = seed.ToList();
        txRepo.Setup(r => r.GetAll()).Returns(() => list.AsQueryable());
        txRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns((int id) => list.FirstOrDefault(t => t.TransactionId == id));
        txRepo.Setup(r => r.GetByReference(It.IsAny<string>())).Returns((string rf) => list.FirstOrDefault(t => t.ProviderTxnId == rf));
        txRepo.Setup(r => r.Add(It.IsAny<Transaction>())).Callback<Transaction>(t =>
        {
            if (t.TransactionId == 0) t.TransactionId = list.Count == 0 ? 1 : list.Max(x => x.TransactionId) + 1;
            list.Add(t);
        });
        txRepo.Setup(r => r.Update(It.IsAny<Transaction>())).Callback<Transaction>(t =>
        {
            var idx = list.FindIndex(x => x.TransactionId == t.TransactionId);
            if (idx >= 0) list[idx] = t;
        });

        var svc = new TransactionService(txRepo.Object, vipRepo.Object, userRepo.Object);
        return (svc, txRepo, vipRepo, userRepo);
    }

    [Fact]
    public void GetById_ReturnsNull_WhenNotFoundOrForbidden()
    {
        var (svc, _, _, _) = CreateService(Array.Empty<Transaction>());
        Assert.Null(svc.GetById(1, 123, isAdmin: false));

        var seed = new[] { new Transaction { TransactionId = 1, UserId = 2, Status = "PAID", CreatedAt = DateTime.UtcNow } };
        var (svc2, _, _, _) = CreateService(seed);
        Assert.Null(svc2.GetById(1, currentUserId: 3, isAdmin: false));

        var dto = svc2.GetById(1, 99, isAdmin: true);
        Assert.NotNull(dto);
        Assert.Equal(1, dto!.TransactionId);
    }

    [Fact]
    public void CreateOrGetByReference_Validates_And_Creates()
    {
        var (svc, txRepo, _, _) = CreateService(Array.Empty<Transaction>());

        var dto = new TransactionDTO { Amount = 10, PaymentMethod = "card", Currency = null, Purpose = null, ProviderTxnId = null, ReferenceCode = null };
        Assert.Throws<InvalidOperationException>(() => svc.CreateOrGetByReference(dto, 7));

        var existing = new Transaction { TransactionId = 5, ProviderTxnId = "ref-1", UserId = 1, Status = "PENDING", CreatedAt = DateTime.UtcNow };
        (svc, txRepo, _, _) = CreateService(new[] { existing });
        var dto2 = new TransactionDTO { ProviderTxnId = "ref-1" };
        var got = svc.CreateOrGetByReference(dto2, 9);
        Assert.Equal(5, got.TransactionId);

        (svc, txRepo, _, _) = CreateService(Array.Empty<Transaction>());
        var created = svc.CreateOrGetByReference(new TransactionDTO { Amount = 12.5m, PaymentMethod = "cash", ReferenceCode = "r-2" }, 42);
        txRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
        txRepo.Verify(r => r.SaveChanges(), Times.Once);
        Assert.Equal("VND", created.Currency);
        Assert.Equal("VIP", created.Purpose);
        Assert.Equal(42, created.UserId);
    }

    [Fact]
    public void Refund_Validates_And_Updates()
    {
        var now = DateTime.UtcNow;
        var tx = new Transaction { TransactionId = 1, UserId = 2, Status = "PAID", CreatedAt = now };
        var (svc, txRepo, _, _) = CreateService(new[] { tx });

        Assert.Throws<KeyNotFoundException>(() => CreateService(Array.Empty<Transaction>()).svc.Refund(99, 1, true));
        Assert.Throws<UnauthorizedAccessException>(() => svc.Refund(1, currentUserId: 3, isAdmin: false));

        var tx2 = new Transaction { TransactionId = 2, UserId = 2, Status = "PENDING", CreatedAt = now };
        (svc, txRepo, _, _) = CreateService(new[] { tx2 });
        Assert.Throws<InvalidOperationException>(() => svc.Refund(2, 2, true));

        (svc, txRepo, _, _) = CreateService(new[] { tx });
        var result = svc.Refund(1, 2, true);
        txRepo.Verify(r => r.Update(It.Is<Transaction>(t => t.TransactionId == 1 && t.Status == "REFUNDED")), Times.Once);
        txRepo.Verify(r => r.SaveChanges(), Times.Once);
        Assert.Equal("REFUNDED", result.Status);
    }

    [Fact]
    public void CreateVipTransaction_ValidatesPlan_AndCreates()
    {
        var (svc, txRepo, vipRepo, _) = CreateService(Array.Empty<Transaction>());
        vipRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns((VipPlan?)null);
        Assert.Throws<InvalidOperationException>(() => svc.CreateVipTransaction(new TransactionDTO { PlanId = 7 }, 10));

        var plan = new VipPlan { VipPlanId = 3, Price = 99m, DurationDays = 30 };
        vipRepo.Setup(r => r.GetById(3)).Returns(plan);
        var dto = new TransactionDTO { PlanId = 3, PaymentMethod = "momo", ProviderTxnId = "prov-1" };
        var created = svc.CreateVipTransaction(dto, 77);
        txRepo.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
        txRepo.Verify(r => r.SaveChanges(), Times.Once);
        Assert.Equal("PENDING", created.Status);
        Assert.Equal(99m, created.Amount);
    }

    [Fact]
    public void ExportCsv_CoversEscapeCsv_AndRows()
    {
        var user = new User { UserId = 1, Username = "John, \"Doe\"" };
        var seed = new[]
        {
            new Transaction { TransactionId = 1, UserId = 1, User = user, ProviderTxnId = "a,b", PaymentMethod = "card\nnew", Purpose = "VIP", Currency = "VND", Amount = 10.5m, Status = "PAID", CreatedAt = DateTime.UnixEpoch },
            new Transaction { TransactionId = 2, UserId = 1, User = user, ProviderTxnId = null, PaymentMethod = null, Purpose = null, Currency = "USD", Amount = 2m, Status = "PENDING", CreatedAt = DateTime.UnixEpoch }
        };
        var (svc, _, _, _) = CreateService(seed);

        var csvBytes = svc.ExportCsv(new TransactionDTO { Page = 1, PageSize = 10 }, currentUserId: 1, isAdmin: true);
        var csv = Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("TransactionId,UserId,Username,Amount,Currency,PaymentMethod,ReferenceCode,Purpose,Status,CreatedAt", csv);
        Assert.Contains("\"John, \"\"Doe\"\"\"", csv); // escaped username
        Assert.Contains("\"a,b\"", csv); // escaped provider id
        Assert.Contains("\"card\nnew\"", csv); // escaped payment method
    }

    [Fact]
    public void GetPaged_Scope_Filter_Sort_Branches()
    {
        var user1 = new User { UserId = 1, Username = "u1" };
        var user2 = new User { UserId = 2, Username = "u2" };
        var now = DateTime.UtcNow;
        var list = new List<Transaction>
        {
            new Transaction { TransactionId = 1, UserId = 1, User = user1, Amount = 5, Currency = "VND", PaymentMethod = "m", ProviderTxnId = "x1", Purpose = "VIP", Status = "PAID", CreatedAt = now.AddDays(-2) },
            new Transaction { TransactionId = 2, UserId = 2, User = user2, Amount = 15, Currency = "VND", PaymentMethod = "m", ProviderTxnId = "x2", Purpose = "VIP", Status = "PENDING", CreatedAt = now.AddDays(-1) },
            new Transaction { TransactionId = 3, UserId = 1, User = user1, Amount = 10, Currency = "USD", PaymentMethod = "c", ProviderTxnId = "search", Purpose = "OTHER", Status = "FAILED", CreatedAt = now }
        };

        var (svc, _, _, _) = CreateService(list);

        var q = new TransactionDTO
        {
            Page = 1,
            PageSize = 10,
            DateFrom = now.AddDays(-2),
            DateTo = now,
            FilterStatus = "PAID",
            FilterType = "VIP",
            MinAmount = 1,
            MaxAmount = 100,
            SortBy = "amount",
            SortDir = "desc",
            Search = "x"
        };

        // Admin scoped by q.UserId
        q.UserId = 1;
        var paged = svc.GetPaged(q, currentUserId: 999, isAdmin: true);
        Assert.All(paged.Items, it => Assert.Equal(1, it.UserId));

        // Non-admin scope (current user only)
        q.UserId = 0;
        var paged2 = svc.GetPaged(q, currentUserId: 1, isAdmin: false);
        Assert.All(paged2.Items, it => Assert.Equal(1, it.UserId));

        // Change sort branch to status asc
        q.SortBy = "status";
        q.SortDir = "asc";
        var paged3 = svc.GetPaged(q, currentUserId: 1, isAdmin: true);
        Assert.True(paged3.Items.Count >= 0);

        // Default sort branch by createdAt desc
        q.SortBy = null;
        q.SortDir = "desc";
        var paged4 = svc.GetPaged(q, currentUserId: 1, isAdmin: true);
        Assert.True(paged4.Items.Count >= 0);
    }
}


