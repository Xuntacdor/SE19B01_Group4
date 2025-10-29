using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Controllers;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class NotificationControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationController _controller;

        public NotificationControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _controller = new NotificationController(_context);

            SeedTestData();
            ClearSession();
        }

        private void SeedTestData()
        {
            // Clear any existing data
            _context.Notification.RemoveRange(_context.Notification);
            _context.User.RemoveRange(_context.User);
            _context.SaveChanges();

            var user1 = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                UserId = 2,
                Username = "otheruser",
                Email = "other@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = "user",
                CreatedAt = DateTime.UtcNow
            };

            _context.User.AddRange(user1, user2);
            _context.SaveChanges();

            var notification1 = new Notification
            {
                NotificationId = 1,
                UserId = 1,
                Content = "Test notification 1",
                Type = "post_approved",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                PostId = null
            };

            var notification2 = new Notification
            {
                NotificationId = 2,
                UserId = 1,
                Content = "Test notification 2",
                Type = "post_rejected",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                PostId = 1
            };

            var notification3 = new Notification
            {
                NotificationId = 3,
                UserId = 2,
                Content = "Other user notification",
                Type = "post_approved",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                PostId = null
            };

            _context.Notification.AddRange(notification1, notification2, notification3);
            _context.SaveChanges();
        }

        private void SetSession(int? userId = 1)
        {
            var context = new DefaultHttpContext();
            var session = new TestSession();
            context.Session = session;
            if (userId.HasValue)
            {
                // Use proper byte conversion for Int32
                var bytes = new byte[4];
                bytes[0] = (byte)(userId.Value & 0xFF);
                bytes[1] = (byte)((userId.Value >> 8) & 0xFF);
                bytes[2] = (byte)((userId.Value >> 16) & 0xFF);
                bytes[3] = (byte)((userId.Value >> 24) & 0xFF);
                session.Set("UserId", bytes);
            }
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        private void ClearSession()
        {
            var context = new DefaultHttpContext();
            context.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext { HttpContext = context };
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ============ GET NOTIFICATIONS ============

        [Fact]
        public void GetNotifications_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.GetNotifications();

            result.Result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result.Result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Please login to view notifications");
        }


        [Fact]
        public void GetNotifications_WhenNoNotifications_ReturnsEmptyList()
        {
            SetSession(1);
            
            // Delete all user 1 notifications
            var userNotifications = _context.Notification.Where(n => n.UserId == 1).ToList();
            _context.Notification.RemoveRange(userNotifications);
            _context.SaveChanges();

            var result = _controller.GetNotifications();

            var ok = result.Result as OkObjectResult;
            var notifications = ok!.Value as List<NotificationDTO>;
            notifications.Should().BeEmpty();
        }

        // ============ MARK AS READ ============

        [Fact]
        public void MarkAsRead_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.MarkAsRead(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Please login to mark notifications as read");
        }


        [Fact]
        public void MarkAsRead_WhenNotificationNotFound_ReturnsNotFound()
        {
            SetSession(1);

            var result = _controller.MarkAsRead(999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Notification not found");
        }

        [Fact]
        public void MarkAsRead_WhenNotificationBelongsToOtherUser_ReturnsNotFound()
        {
            SetSession(1);

            var result = _controller.MarkAsRead(3); // Belongs to user 2

            result.Should().BeOfType<NotFoundObjectResult>();
        }


        // ============ DELETE NOTIFICATION ============

        [Fact]
        public void DeleteNotification_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            ClearSession();

            var result = _controller.DeleteNotification(1);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorized = result as UnauthorizedObjectResult;
            unauthorized!.Value.Should().Be("Please login to delete notifications");
        }


        [Fact]
        public void DeleteNotification_WhenNotificationNotFound_ReturnsNotFound()
        {
            SetSession(1);

            var result = _controller.DeleteNotification(999);

            result.Should().BeOfType<NotFoundObjectResult>();
            var notFound = result as NotFoundObjectResult;
            notFound!.Value.Should().Be("Notification not found");
        }

        [Fact]
        public void DeleteNotification_WhenNotificationBelongsToOtherUser_ReturnsNotFound()
        {
            SetSession(1);

            var result = _controller.DeleteNotification(3); // Belongs to user 2

            result.Should().BeOfType<NotFoundObjectResult>();
            
            // Verify notification still exists
            var notification = _context.Notification.Find(3);
            notification.Should().NotBeNull();
        }


        // ============ EDGE CASES ============

    }
}

