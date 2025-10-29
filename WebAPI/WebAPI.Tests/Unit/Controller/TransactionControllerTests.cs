using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Tests.Unit.Controller;

public class TransactionControllerTests
{
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
    }

    private static TransactionController CreateController(Mock<ITransactionService> serviceMock,
        int? sessionUserId = null, string? sessionRole = null,
        bool useClaims = false, int? claimUserId = null, string? claimRole = null)
    {
        var controller = new TransactionController(serviceMock.Object);
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Session = session;

        if (sessionUserId.HasValue)
        {
            session.Set("UserId", BitConverter.GetBytes(sessionUserId.Value));
        }
        if (sessionRole != null)
        {
            session.Set("Role", Encoding.UTF8.GetBytes(sessionRole));
        }

        if (useClaims)
        {
            var claims = new List<Claim>();
            if (claimUserId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, claimUserId.Value.ToString()));
            }
            if (!string.IsNullOrEmpty(claimRole))
            {
                claims.Add(new Claim(ClaimTypes.Role, claimRole));
            }
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            // Ensure session path goes to claims by not setting session UserId
            session.Remove("UserId");
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public void GetPaged_Unauthorized_WhenNoUser()
    {
        var service = new Mock<ITransactionService>();
        var controller = CreateController(service, sessionUserId: null, sessionRole: null);
        var result = controller.GetPaged(new TransactionDTO());
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GetPaged_ReturnsOk_WhenUserFromSession()
    {
        var service = new Mock<ITransactionService>();
        service.Setup(s => s.GetPaged(It.IsAny<TransactionDTO>(), 1, true))
               .Returns(new PagedResult<TransactionDTO> { Items = new List<TransactionDTO>(), Page = 1, PageSize = 10, Total = 0 });

        var controller = CreateController(service, sessionUserId: 1, sessionRole: "admin");
        var result = controller.GetPaged(new TransactionDTO());
        var ok = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetById_Unauthorized_WhenNoUser()
    {
        var service = new Mock<ITransactionService>();
        var controller = CreateController(service, sessionUserId: null);
        var result = controller.GetById(5);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GetById_NotFound_WhenServiceReturnsNull()
    {
        var service = new Mock<ITransactionService>();
        service.Setup(s => s.GetById(5, 2, true)).Returns((TransactionDTO?)null);
        var controller = CreateController(service, sessionUserId: 2, sessionRole: "admin");
        var result = controller.GetById(5);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetById_Ok_WhenFound()
    {
        var service = new Mock<ITransactionService>();
        service.Setup(s => s.GetById(5, 2, false)).Returns(new TransactionDTO { TransactionId = 5 });
        var controller = CreateController(service, useClaims: true, claimUserId: 2, claimRole: "user");
        var result = controller.GetById(5);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TransactionDTO>(ok.Value);
        Assert.Equal(5, dto.TransactionId);
    }

    [Fact]
    public void Post_Unauthorized_WhenNoUser()
    {
        var service = new Mock<ITransactionService>();
        var controller = CreateController(service);
        var result = controller.Post(new TransactionDTO());
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Post_Created_WhenServiceOk()
    {
        var service = new Mock<ITransactionService>();
        service.Setup(s => s.CreateOrGetByReference(It.IsAny<TransactionDTO>(), 3))
               .Returns(new TransactionDTO { TransactionId = 123 });
        var controller = CreateController(service, useClaims: true, claimUserId: 3, claimRole: "admin");
        var result = controller.Post(new TransactionDTO());
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<TransactionDTO>(created.Value);
        Assert.Equal(123, dto.TransactionId);
    }

    [Fact]
    public void Refund_Unauthorized_WhenNoUser()
    {
        var service = new Mock<ITransactionService>();
        var controller = CreateController(service);
        var result = controller.Refund(9);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Refund_Ok_WhenServiceOk_Conflict_OnException()
    {
        var serviceOk = new Mock<ITransactionService>();
        serviceOk.Setup(s => s.Refund(9, 4, true)).Returns(new TransactionDTO { TransactionId = 9 });
        var ctrlOk = CreateController(serviceOk, sessionUserId: 4, sessionRole: "admin");
        var okRes = ctrlOk.Refund(9);
        Assert.IsType<OkObjectResult>(okRes);

        var serviceFail = new Mock<ITransactionService>();
        serviceFail.Setup(s => s.Refund(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>())).Throws(new Exception("boom"));
        var ctrlFail = CreateController(serviceFail, sessionUserId: 4, sessionRole: "admin");
        var conf = ctrlFail.Refund(9);
        Assert.IsType<ConflictObjectResult>(conf);
    }

    [Fact]
    public void CreateTransaction_Unauthorized_WhenNoUser()
    {
        var service = new Mock<ITransactionService>();
        var controller = CreateController(service);
        var result = controller.CreateTransaction(new TransactionDTO());
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void CreateTransaction_Ok_WhenServiceOk()
    {
        var service = new Mock<ITransactionService>();
        service.Setup(s => s.CreateVipTransaction(It.IsAny<TransactionDTO>(), 7))
               .Returns(new TransactionDTO { TransactionId = 77 });
        var controller = CreateController(service, useClaims: true, claimUserId: 7, claimRole: "user");
        var result = controller.CreateTransaction(new TransactionDTO());
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TransactionDTO>(ok.Value);
        Assert.Equal(77, dto.TransactionId);
    }

    [Fact]
    public void Export_Unauthorized_WhenNoUser()
    {
        var service = new Mock<ITransactionService>();
        var controller = CreateController(service);
        var result = controller.Export(new TransactionDTO());
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Export_File_WhenServiceOk()
    {
        var service = new Mock<ITransactionService>();
        service.Setup(s => s.ExportCsv(It.IsAny<TransactionDTO>(), 11, true))
               .Returns(Encoding.UTF8.GetBytes("csv"));
        var controller = CreateController(service, sessionUserId: 11, sessionRole: "admin");
        var result = controller.Export(new TransactionDTO());
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.NotNull(file.FileContents);
    }
}


