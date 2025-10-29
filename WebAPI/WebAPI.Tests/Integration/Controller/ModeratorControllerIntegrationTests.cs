using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Repositories;
using WebAPI.Services;
using WebAPI.ExternalServices;
using Xunit;

namespace WebAPI.Tests.Integration.Controller
{
    public class ModeratorControllerIntegrationTests : IntegrationTestBase
    {
        private ModeratorController CreateController(int? userId = null, string role = "user")
        {
            var userRepository = new UserRepository(_context);

            var postService = new PostService(_context, userRepository);
            var mockEmailService = new Mock<IEmailService>();
            var mockOtpService = new Mock<IOtpService>();
            var userService = new UserService(userRepository, mockEmailService.Object, _context, mockOtpService.Object);
            var commentService = new CommentService(_context, userRepository);

            var controller = new ModeratorController(postService, userService, commentService);

            if (userId.HasValue)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = CreateHttpContextWithSession(userId.Value, role)
                };
            }
            else
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = CreateHttpContextWithoutAuth()
                };
            }

            return controller;
        }

        // ============ GET REPORTED COMMENTS ============

        [Fact]
        public void GetReportedComments_WithModeratorRole_ReturnsOkWithComments()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.GetReportedComments(1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var comments = okResult!.Value as IEnumerable<ReportedCommentDTO>;
            comments.Should().NotBeNull();
            comments.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void GetReportedComments_WithAdminRole_ReturnsOkWithComments()
        {
            var controller = CreateController(2, "admin");

            var result = controller.GetReportedComments(1, 10);

            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var comments = okResult!.Value as IEnumerable<ReportedCommentDTO>;
            comments.Should().NotBeNull();
        }

        [Fact]
        public void GetReportedComments_WithUserRole_ReturnsUnauthorized()
        {
            var controller = CreateController(3, "user");

            var result = controller.GetReportedComments(1, 10);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result.Result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Access denied. Moderator or Admin role required.");
        }

        [Fact]
        public void GetReportedComments_NotLoggedIn_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = controller.GetReportedComments(1, 10);

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // ============ APPROVE REPORT ============

        [Fact]
        public void ApproveReport_WithModeratorRole_ReturnsOk()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.ApproveReport(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Report approved successfully" });

            // Verify report status changed
            var report = _context.Report.Find(1);
            report.Should().NotBeNull();
            report!.Status.Should().Be("Approved");
        }

        [Fact]
        public void ApproveReport_WithAdminRole_ReturnsOk()
        {
            var controller = CreateController(2, "admin");

            var result = controller.ApproveReport(2);

            result.Should().BeOfType<OkObjectResult>();

            // Verify report status changed
            var report = _context.Report.Find(2);
            report!.Status.Should().Be("Approved");
        }

        [Fact]
        public void ApproveReport_WithUserRole_ReturnsUnauthorized()
        {
            var controller = CreateController(3, "user");

            var result = controller.ApproveReport(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result as UnauthorizedObjectResult;
            var value = unauthorized!.Value?.ToString();
            value.Should().Contain("Access denied");
        }

        [Fact]
        public void ApproveReport_NotLoggedIn_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = controller.ApproveReport(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("User not authenticated. Please login again.");
        }

        [Fact]
        public void ApproveReport_WhenReportNotFound_ReturnsNotFound()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.ApproveReport(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ============ DISMISS REPORT ============

        [Fact]
        public void DismissReport_WithModeratorRole_ReturnsOk()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.DismissReport(1);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "Report dismissed successfully" });

            // Verify report status changed
            var report = _context.Report.Find(1);
            report.Should().NotBeNull();
            report!.Status.Should().Be("Dismissed");
        }

        [Fact]
        public void DismissReport_WithAdminRole_ReturnsOk()
        {
            var controller = CreateController(2, "admin");

            var result = controller.DismissReport(2);

            result.Should().BeOfType<OkObjectResult>();

            // Verify report status changed
            var report = _context.Report.Find(2);
            report!.Status.Should().Be("Dismissed");
        }

        [Fact]
        public void DismissReport_WithUserRole_ReturnsUnauthorized()
        {
            var controller = CreateController(3, "user");

            var result = controller.DismissReport(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void DismissReport_NotLoggedIn_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = controller.DismissReport(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void DismissReport_WhenReportNotFound_ReturnsNotFound()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.DismissReport(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ============ RESTRICT USER ============

        [Fact]
        public void RestrictUser_WithModeratorRole_ReturnsOk()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.RestrictUser(3);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "User restricted successfully" });

            // Verify user is restricted
            var user = _context.User.Find(3);
            user.Should().NotBeNull();
            user!.IsRestricted.Should().BeTrue();
        }

        [Fact]
        public void RestrictUser_WithAdminRole_ReturnsOk()
        {
            var controller = CreateController(2, "admin");

            var result = controller.RestrictUser(3);

            result.Should().BeOfType<OkObjectResult>();

            // Verify user is restricted
            var user = _context.User.Find(3);
            user!.IsRestricted.Should().BeTrue();
        }

        [Fact]
        public void RestrictUser_WithUserRole_ReturnsUnauthorized()
        {
            var controller = CreateController(3, "user");

            var result = controller.RestrictUser(4);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Only moderators and admins can restrict users");
        }

        [Fact]
        public void RestrictUser_NotLoggedIn_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = controller.RestrictUser(3);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void RestrictUser_WhenUserNotFound_ReturnsNotFound()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.RestrictUser(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ============ UNRESTRICT USER ============

        [Fact]
        public void UnrestrictUser_WithModeratorRole_ReturnsOk()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.UnrestrictUser(4);

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { message = "User unrestricted successfully" });

            // Verify user is unrestricted
            var user = _context.User.Find(4);
            user.Should().NotBeNull();
            user!.IsRestricted.Should().BeFalse();
        }

        [Fact]
        public void UnrestrictUser_WithAdminRole_ReturnsOk()
        {
            var controller = CreateController(2, "admin");

            var result = controller.UnrestrictUser(4);

            result.Should().BeOfType<OkObjectResult>();

            // Verify user is unrestricted
            var user = _context.User.Find(4);
            user!.IsRestricted.Should().BeFalse();
        }

        [Fact]
        public void UnrestrictUser_WithUserRole_ReturnsUnauthorized()
        {
            var controller = CreateController(3, "user");

            var result = controller.UnrestrictUser(4);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UnrestrictUser_NotLoggedIn_ReturnsUnauthorized()
        {
            var controller = CreateController();

            var result = controller.UnrestrictUser(4);

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void UnrestrictUser_WhenUserNotFound_ReturnsNotFound()
        {
            var controller = CreateController(1, "moderator");

            var result = controller.UnrestrictUser(999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}

