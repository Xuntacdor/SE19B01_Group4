using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using WebAPI.Data;
using WebAPI.Models;
using Xunit;

namespace WebAPI.Tests.Integration
{
    public class IntegrationTestBase : IDisposable
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IServiceProvider _serviceProvider;

        public IntegrationTestBase()
        {
            var services = new ServiceCollection();

            // Setup in-memory database with transaction warning suppression
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                       .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            // Add HTTP context accessor for session management
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();

            SeedDatabase();
        }

        protected virtual void SeedDatabase()
        {
            // Seed test users
            var moderator = new User
            {
                UserId = 1,
                Username = "moderator",
                Email = "moderator@test.com",
                Role = "moderator",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                CreatedAt = DateTime.UtcNow
            };

            var admin = new User
            {
                UserId = 2,
                Username = "admin",
                Email = "admin@test.com",
                Role = "admin",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                CreatedAt = DateTime.UtcNow
            };

            var regularUser = new User
            {
                UserId = 3,
                Username = "user",
                Email = "user@test.com",
                Role = "user",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                CreatedAt = DateTime.UtcNow
            };

            var restrictedUser = new User
            {
                UserId = 4,
                Username = "restricted",
                Email = "restricted@test.com",
                Role = "user",
                IsRestricted = true,
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                CreatedAt = DateTime.UtcNow
            };

            _context.User.AddRange(moderator, admin, regularUser, restrictedUser);

            // Seed test posts
            var post1 = new Post
            {
                PostId = 1,
                UserId = 3,
                Title = "Test Post 1",
                Content = "Content 1",
                Status = "approved",
                CreatedAt = DateTime.UtcNow
            };

            var post2 = new Post
            {
                PostId = 2,
                UserId = 3,
                Title = "Test Post 2",
                Content = "Content 2",
                Status = "approved",
                CreatedAt = DateTime.UtcNow
            };

            _context.Post.AddRange(post1, post2);

            // Seed test comments
            var comment1 = new Comment
            {
                CommentId = 1,
                PostId = 1,
                UserId = 3,
                Content = "Test comment 1",
                CreatedAt = DateTime.UtcNow
            };

            var comment2 = new Comment
            {
                CommentId = 2,
                PostId = 1,
                UserId = 3,
                Content = "Test comment 2",
                CreatedAt = DateTime.UtcNow
            };

            _context.Comment.AddRange(comment1, comment2);

            // Seed test reports
            var report1 = new Report
            {
                ReportId = 1,
                UserId = 3,
                CommentId = 1,
                CommentAuthorUserId = 3,
                Content = "Inappropriate content",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var report2 = new Report
            {
                ReportId = 2,
                UserId = 3,
                CommentId = 2,
                CommentAuthorUserId = 3,
                Content = "Spam",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Report.AddRange(report1, report2);

            _context.SaveChanges();
        }

        protected HttpContext CreateHttpContextWithSession(int userId, string role)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            
            // Set session
            var bytes = new byte[4];
            bytes[0] = (byte)(userId & 0xFF);
            bytes[1] = (byte)((userId >> 8) & 0xFF);
            bytes[2] = (byte)((userId >> 16) & 0xFF);
            bytes[3] = (byte)((userId >> 24) & 0xFF);
            httpContext.Session.Set("UserId", bytes);

            // Set claims for JWT-based authentication
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, $"user{userId}")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);

            return httpContext;
        }

        protected HttpContext CreateHttpContextWithoutAuth()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            return httpContext;
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new Dictionary<string, byte[]>();

        public bool IsAvailable => true;

        public string Id => Guid.NewGuid().ToString();

        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }
}

