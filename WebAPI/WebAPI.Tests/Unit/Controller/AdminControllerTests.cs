using System;
using System.Collections.Generic;
using System.Linq;
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
    public class AdminControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminController(_userServiceMock.Object, _adminServiceMock.Object);
        }

        [Fact]
        public void RegisterAdmin_ReturnsCreated_WhenSuccess()
        {
            var dto = new RegisterRequestDTO { Username = "admin", Email = "a@b.com", Password = "123" };
            var user = new User { UserId = 1, Username = "admin", Email = "a@b.com", Role = "admin" };
            _userServiceMock.Setup(s => s.RegisterAdmin(dto)).Returns(user);

            var result = _controller.RegisterAdmin(dto).Result as CreatedResult;

            result.Should().NotBeNull();
            var value = result!.Value as UserDTO;
            value.Should().NotBeNull();
            value!.Username.Should().Be("admin");
            result.StatusCode.Should().Be(201);
        }

        [Fact]
        public void RegisterAdmin_ReturnsConflict_WhenDuplicate()
        {
            var dto = new RegisterRequestDTO();
            _userServiceMock.Setup(s => s.RegisterAdmin(dto)).Throws(new InvalidOperationException("duplicate"));

            var result = _controller.RegisterAdmin(dto).Result as ConflictObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("duplicate");
        }

        [Fact]
        public void RegisterAdmin_ReturnsBadRequest_WhenModelInvalid()
        {
            _controller.ModelState.AddModelError("Email", "Required");
            var result = _controller.RegisterAdmin(new RegisterRequestDTO()).Result as BadRequestObjectResult;
            result.Should().NotBeNull();
        }

        [Fact]
        public void GrantRole_ReturnsNotFound_WhenUserMissing()
        {
            _userServiceMock.Setup(s => s.GetById(1)).Returns((User)null);

            var result = _controller.GrantRole(1, "user") as NotFoundObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("User not found.");
        }

        [Fact]
        public void GrantRole_ReturnsBadRequest_WhenTargetIsAdmin()
        {
            _userServiceMock.Setup(s => s.GetById(1)).Returns(new User { Role = "admin" });

            var result = _controller.GrantRole(1, "user") as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("Cannot modify another admin.");
        }

        [Fact]
        public void GrantRole_ReturnsBadRequest_WhenInvalidRole()
        {
            _userServiceMock.Setup(s => s.GetById(1)).Returns(new User { Role = "user" });

            var result = _controller.GrantRole(1, "invalid") as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("Invalid role. Allowed values: user, moderator.");
        }

        [Fact]
        public void GrantRole_ReturnsOk_WhenSuccess()
        {
            var user = new User { UserId = 1, Username = "john", Role = "user" };
            _userServiceMock.Setup(s => s.GetById(1)).Returns(user);

            var result = _controller.GrantRole(1, "moderator") as OkObjectResult;

            result.Should().NotBeNull();
            var value = result!.Value?.GetType().GetProperty("message")?.GetValue(result.Value, null)?.ToString();

            value.Should().Contain("john");
            value.Should().Contain("moderator");
            _userServiceMock.Verify(s => s.Update(It.Is<User>(u => u.Role == "moderator")), Times.Once);
        }

        [Fact]
        public void GetAllUsers_ReturnsList()
        {
            var users = new List<User>
            {
                new User { UserId = 1, Username = "a" },
                new User { UserId = 2, Username = "b" }
            };
            _userServiceMock.Setup(s => s.GetAll()).Returns(users);

            var result = _controller.GetAllUsers().Result as OkObjectResult;

            result.Should().NotBeNull();
            var list = result!.Value as List<UserDTO>;
            list.Should().HaveCount(2);
        }

        [Fact]
        public void GetUserById_ReturnsNotFound_WhenMissing()
        {
            _userServiceMock.Setup(s => s.GetById(10)).Returns((User)null);

            var result = _controller.GetUserById(10).Result as NotFoundObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("User not found.");
        }

        [Fact]
        public void GetUserById_ReturnsOk_WhenExists()
        {
            var user = new User { UserId = 1, Username = "bob", Email = "b@c.com", Role = "user" };
            _userServiceMock.Setup(s => s.GetById(1)).Returns(user);

            var result = _controller.GetUserById(1).Result as OkObjectResult;

            result.Should().NotBeNull();
            var dto = result!.Value as UserDTO;
            dto.Should().NotBeNull();
            dto!.Username.Should().Be("bob");
        }

        [Fact]
        public void GetDashboardStats_ReturnsCorrectValues()
        {
            _adminServiceMock.Setup(s => s.GetDashboardStats())
                .Returns((5, 3, 2000m, 10));

            var result = _controller.GetDashboardStats() as OkObjectResult;

            result.Should().NotBeNull();
            var value = result!.Value!;
            var props = value.GetType().GetProperties();

            var dict = props.ToDictionary(p => p.Name, p => p.GetValue(value));

            dict["totalUsers"].Should().Be(5);
            dict["totalExams"].Should().Be(3);
            dict["totalTransactions"].Should().Be(2000m);
            dict["totalAttempts"].Should().Be(10);
        }

        [Fact]
        public void GetSalesTrend_ReturnsTrendData()
        {
            var trend = new List<object>
            {
                new { year = 2025, month = 10, total = 1000m },
                new { year = 2025, month = 11, total = 2000m }
            };
            _adminServiceMock.Setup(s => s.GetSalesTrend()).Returns(trend);

            var result = _controller.GetSalesTrend() as OkObjectResult;

            result.Should().NotBeNull();
            var list = result!.Value as IEnumerable<object>;
            list.Should().HaveCount(2);
        }
    }
}
