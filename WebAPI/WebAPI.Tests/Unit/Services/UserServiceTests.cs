using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.ExternalServices;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _repoMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<IOtpService> _otpMock;
        private readonly ApplicationDbContext _context;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _repoMock = new Mock<IUserRepository>();
            _emailMock = new Mock<IEmailService>();
            _otpMock = new Mock<IOtpService>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _service = new UserService(_repoMock.Object, _emailMock.Object, _context, _otpMock.Object);
        }

        // ------------------------------------------------------------
        // Register
        // ------------------------------------------------------------
        [Fact]
        public void Register_ShouldCreateUser_WhenEmailNotExists()
        {
            _repoMock.Setup(r => r.GetByEmail(It.IsAny<string>())).Returns((User)null);

            var dto = new RegisterRequestDTO
            {
                Username = "user1",
                Email = "test@x.com",
                Password = "pass",
                Firstname = "A",
                Lastname = "B"
            };

            var user = _service.Register(dto);

            user.Should().NotBeNull();
            _repoMock.Verify(r => r.Add(It.IsAny<User>()), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Register_ShouldThrow_WhenEmailExists()
        {
            _repoMock.Setup(r => r.GetByEmail("dup@x.com")).Returns(new User());

            var dto = new RegisterRequestDTO
            {
                Email = "dup@x.com",
                Password = "123",
                Username = "u"
            };

            Action act = () => _service.Register(dto);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RegisterAdmin_ShouldSetRoleAdmin()
        {
            _repoMock.Setup(r => r.GetByEmail(It.IsAny<string>())).Returns((User)null);

            var dto = new RegisterRequestDTO
            {
                Email = "a@b.com",
                Password = "123",
                Username = "adm"
            };

            var u = _service.RegisterAdmin(dto);
            u.Role.Should().Be("admin");
        }

        // ------------------------------------------------------------
        // Authenticate
        // ------------------------------------------------------------
        [Fact]
        public void Authenticate_ShouldReturnUser_WhenCredentialsValid()
        {
            PasswordService.CreatePasswordHash("p", out var h, out var s);
            var u = new User { Email = "e", PasswordHash = h, PasswordSalt = s };
            _repoMock.Setup(r => r.GetByEmail("e")).Returns(u);

            var res = _service.Authenticate("e", "p");
            res.Should().NotBeNull();
        }

        [Fact]
        public void Authenticate_ShouldThrow_WhenEmailNotFound()
        {
            _repoMock.Setup(r => r.GetByEmail("nope")).Returns((User)null);
            Action act = () => _service.Authenticate("nope", "p");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        [Fact]
        public void Authenticate_ShouldThrow_WhenPasswordWrong()
        {
            PasswordService.CreatePasswordHash("real", out var h, out var s);
            _repoMock.Setup(r => r.GetByEmail("e")).Returns(new User { Email = "e", PasswordHash = h, PasswordSalt = s });

            Action act = () => _service.Authenticate("e", "wrong");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        // ------------------------------------------------------------
        // Update
        // ------------------------------------------------------------
        [Fact]
        public void Update_ShouldModifyFields_WhenSelfUser()
        {
            var user = new User { UserId = 1, Email = "old@x.com", Role = "user" };
            _repoMock.Setup(r => r.GetById(1)).Returns(user);

            var dto = new UpdateUserDTO { Firstname = "New", Email = "old@x.com" };

            _service.Update(1, dto, 1);

            user.Firstname.Should().Be("New");
            _repoMock.Verify(r => r.Update(user), Times.Once);
        }

        [Fact]
        public void Update_ShouldThrow_WhenNotAdminOrSelf()
        {
            var user = new User { UserId = 2, Role = "user" };
            var current = new User { UserId = 1, Role = "user" };

            _repoMock.Setup(r => r.GetById(2)).Returns(user);
            _repoMock.Setup(r => r.GetById(1)).Returns(current);

            var dto = new UpdateUserDTO();

            Action act = () => _service.Update(2, dto, 1);
            act.Should().Throw<UnauthorizedAccessException>();
        }

        // ------------------------------------------------------------
        // Delete
        // ------------------------------------------------------------
        [Fact]
        public void Delete_ShouldRemove_WhenAdmin()
        {
            var u = new User { UserId = 5, Role = "user" };
            var admin = new User { UserId = 1, Role = "admin" };
            _repoMock.Setup(r => r.GetById(5)).Returns(u);
            _repoMock.Setup(r => r.GetById(1)).Returns(admin);

            _service.Delete(5, 1);
            _repoMock.Verify(r => r.Delete(u), Times.Once);
        }

        [Fact]
        public void Delete_ShouldThrow_WhenUnauthorized()
        {
            var u = new User { UserId = 5, Role = "user" };
            var other = new User { UserId = 2, Role = "user" };
            _repoMock.Setup(r => r.GetById(5)).Returns(u);
            _repoMock.Setup(r => r.GetById(2)).Returns(other);

            Action act = () => _service.Delete(5, 2);
            act.Should().Throw<UnauthorizedAccessException>();
        }

        // ------------------------------------------------------------
        // OTP + Reset flow
        // ------------------------------------------------------------
        [Fact]
        public void SendPasswordResetOtp_ShouldReturnSuccess()
        {
            _repoMock.Setup(r => r.GetByEmail("a@b.com")).Returns(new User());
            _otpMock.Setup(o => o.GenerateOtp("a@b.com")).Returns("9999");

            var res = _service.SendPasswordResetOtp("a@b.com");

            res.Should().Contain("OTP");
            _emailMock.Verify(e => e.SendOtpEmail("a@b.com", "9999"), Times.Once);
        }

        [Fact]
        public void SendPasswordResetOtp_ShouldThrow_WhenEmailMissing()
        {
            _repoMock.Setup(r => r.GetByEmail("x")).Returns((User)null);
            Action act = () => _service.SendPasswordResetOtp("x");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void VerifyOtp_ShouldReturnToken_WhenValid()
        {
            _otpMock.Setup(o => o.VerifyOtp("e", "1")).Returns(true);
            _otpMock.Setup(o => o.GenerateResetToken("e")).Returns("tok");

            var res = _service.VerifyOtp("e", "1");
            res.Should().Be("tok");
        }

        [Fact]
        public void VerifyOtp_ShouldThrow_WhenInvalid()
        {
            _otpMock.Setup(o => o.VerifyOtp("e", "1")).Returns(false);
            Action act = () => _service.VerifyOtp("e", "1");
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ResetPassword_ShouldUpdate_WhenValid()
        {
            _otpMock.Setup(o => o.VerifyResetToken("e", "t")).Returns(true);
            var u = new User { Email = "e" };
            _repoMock.Setup(r => r.GetByEmail("e")).Returns(u);

            var msg = _service.ResetPassword("e", "t", "new");
            msg.Should().Contain("success");
            _repoMock.Verify(r => r.Update(u), Times.Once);
        }

        [Fact]
        public void ResetPassword_ShouldThrow_WhenTokenInvalid_Branch2()
        {
            _otpMock.Setup(o => o.VerifyResetToken("e", "t")).Returns(false);
            Action act = () => _service.ResetPassword("e", "t", "new");
            act.Should().Throw<InvalidOperationException>();
        }

        // ------------------------------------------------------------
        // ChangePassword
        // ------------------------------------------------------------
        [Fact]
        public void ChangePassword_ShouldUpdate_WhenCorrect()
        {
            PasswordService.CreatePasswordHash("old", out var h, out var s);
            var u = new User { UserId = 1, PasswordHash = h, PasswordSalt = s };
            _repoMock.Setup(r => r.GetById(1)).Returns(u);

            var msg = _service.ChangePassword(1, "old", "new");
            msg.Should().Contain("success");
            _repoMock.Verify(r => r.Update(u), Times.Once);
        }

        [Fact]
        public void ChangePassword_ShouldThrow_WhenWrongPassword()
        {
            PasswordService.CreatePasswordHash("old", out var h, out var s);
            var u = new User { UserId = 1, PasswordHash = h, PasswordSalt = s };
            _repoMock.Setup(r => r.GetById(1)).Returns(u);

            Action act = () => _service.ChangePassword(1, "bad", "new");
            act.Should().Throw<UnauthorizedAccessException>();
        }

        // ------------------------------------------------------------
        // VIP
        // ------------------------------------------------------------
        [Fact]
        public void IsVip_ShouldReturnTrue_WhenNotExpired()
        {
            var u = new User { VipExpireAt = DateTime.UtcNow.AddDays(1) };
            _repoMock.Setup(r => r.GetById(1)).Returns(u);
            _service.IsVip(1).Should().BeTrue();
        }

        [Fact]
        public void IsVip_ShouldReturnFalse_WhenExpired()
        {
            var u = new User { VipExpireAt = DateTime.UtcNow.AddDays(-1) };
            _repoMock.Setup(r => r.GetById(1)).Returns(u);
            _service.IsVip(1).Should().BeFalse();
        }

        // ------------------------------------------------------------
        // Restrict / Unrestrict
        // ------------------------------------------------------------
        [Fact]
        public void RestrictUser_ShouldCreateNotification()
        {
            var u = new User { UserId = 3 };
            _repoMock.Setup(r => r.GetById(3)).Returns(u);

            _service.RestrictUser(3);

            _context.Notification.Count().Should().Be(1);
            u.IsRestricted.Should().BeTrue();
        }

        [Fact]
        public void UnrestrictUser_ShouldCreateNotification()
        {
            var u = new User { UserId = 3 };
            _repoMock.Setup(r => r.GetById(3)).Returns(u);

            _service.UnrestrictUser(3);

            _context.Notification.Single().Type.Should().Be("account_unrestricted");
            u.IsRestricted.Should().BeFalse();
        }

        // ------------------------------------------------------------
        // Stats
        // ------------------------------------------------------------
        [Fact]
        public void GetUsersWithStats_ShouldReturnPagedResults()
        {
            PasswordService.CreatePasswordHash("p", out var h, out var s);

            var user = new User
            {
                UserId = 1,
                Username = "U",
                Email = "E",
                Role = "user",
                PasswordHash = h,
                PasswordSalt = s,
                Posts = new List<Post>
        {
            new Post
            {
                PostId = 10,
                Title = "Test Post",
                Content = "Post body",
                Status = "approved"
            }
        },
                Comments = new List<Comment>()
            };

            _context.User.Add(user);
            _context.SaveChanges();

            var res = _service.GetUsersWithStats(1, 10).ToList();

            res.Should().HaveCount(1);
            res[0].ApprovedPosts.Should().Be(1);
        }



        [Fact]
        public void GetUserStats_ShouldReturnNull_WhenNotFound()
        {
            var res = _service.GetUserStats(99);
            res.Should().BeNull();
        }

        [Fact]
        public void GetUserProfileStats_ShouldReturnVoteSum()
        {
            PasswordService.CreatePasswordHash("p", out var h, out var s);

            var user = new User
            {
                UserId = 1,
                Username = "x",
                Role = "user",
                Email = "x@x.com",
                PasswordHash = h,
                PasswordSalt = s,
                Posts = new List<Post>()
            };

            var post = new Post
            {
                PostId = 10,
                Title = "Hello",
                Content = "World",
                PostLikes = new List<PostLike>
        {
            new PostLike { UserId = 1, PostId = 10 },
            new PostLike { UserId = 2, PostId = 10 } 
        }
            };

            user.Posts.Add(post);
            _context.User.Add(user);
            _context.SaveChanges();

            var res = _service.GetUserProfileStats(1);

            res.Should().NotBeNull();
            res!.TotalVotes.Should().Be(2);
        }
        [Fact]
        public void GetById_ShouldReturnRepoResult()
        {
            var user = new User { UserId = 1 };
            _repoMock.Setup(r => r.GetById(1)).Returns(user);

            var result = _service.GetById(1);
            result.Should().Be(user);
        }

        [Fact]
        public void GetByEmail_ShouldReturnRepoResult()
        {
            var user = new User { Email = "a@b.com" };
            _repoMock.Setup(r => r.GetByEmail("a@b.com")).Returns(user);

            _service.GetByEmail("a@b.com").Should().Be(user);
        }

        [Fact]
        public void GetAll_ShouldReturnList()
        {
            var list = new List<User> { new User { UserId = 1 } };
            _repoMock.Setup(r => r.GetAll()).Returns(list);

            _service.GetAll().Should().ContainSingle();
        }

        [Fact]
        public void Exists_ShouldReturnTrue_WhenRepoSaysTrue()
        {
            _repoMock.Setup(r => r.Exists(1)).Returns(true);
            _service.Exists(1).Should().BeTrue();
        }

        // ------------------------------------------------------------
        // Register – duplicate branch (already used email)
        // ------------------------------------------------------------
        [Fact]
        public void Register_ShouldThrow_WhenEmailAlreadyUsed()
        {
            _repoMock.Setup(r => r.GetByEmail("used@x.com")).Returns(new User { Email = "used@x.com" });

            var dto = new RegisterRequestDTO
            {
                Email = "used@x.com",
                Password = "123",
                Username = "dup"
            };

            Action act = () => _service.Register(dto);
            act.Should().Throw<InvalidOperationException>().WithMessage("*already been used*");
        }

        // ------------------------------------------------------------
        // Update – cover role/password/email branches
        // ------------------------------------------------------------
        [Fact]
        public void Update_ShouldChangeRole_WhenAdmin()
        {
            var target = new User { UserId = 1, Role = "user", Email = "a@b.com" };
            var admin = new User { UserId = 99, Role = "admin" };

            _repoMock.Setup(r => r.GetById(1)).Returns(target);
            _repoMock.Setup(r => r.GetById(99)).Returns(admin);

            var dto = new UpdateUserDTO { Role = "moderator" };

            _service.Update(1, dto, 99);
            target.Role.Should().Be("moderator");
        }

        [Fact]
        public void Update_ShouldChangePassword_WhenProvided()
        {
            var target = new User { UserId = 1, Role = "user", Email = "a@b.com" };
            _repoMock.Setup(r => r.GetById(1)).Returns(target);
            _repoMock.Setup(r => r.GetById(1)).Returns(target);

            var dto = new UpdateUserDTO { Password = "newpass" };

            _service.Update(1, dto, 1);
            target.PasswordHash.Should().NotBeNull();
            target.PasswordSalt.Should().NotBeNull();
        }

        [Fact]
        public void Update_ShouldThrow_WhenEmailAlreadyTakenByOtherUser()
        {
            var current = new User { UserId = 1, Role = "admin" };
            var target = new User { UserId = 2, Email = "old@x.com" };
            var existing = new User { UserId = 3, Email = "dup@x.com" };

            _repoMock.Setup(r => r.GetById(2)).Returns(target);
            _repoMock.Setup(r => r.GetById(1)).Returns(current);
            _repoMock.Setup(r => r.GetByEmail("dup@x.com")).Returns(existing);

            var dto = new UpdateUserDTO { Email = "dup@x.com" };

            Action act = () => _service.Update(2, dto, 1);
            act.Should().Throw<InvalidOperationException>();
        }

        // ------------------------------------------------------------
        // Update(User) – trivial branch
        // ------------------------------------------------------------
        [Fact]
        public void UpdateUser_ShouldCallRepoUpdateAndSave()
        {
            var u = new User { UserId = 5 };
            _service.Update(u);
            _repoMock.Verify(r => r.Update(u), Times.Once);
            _repoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void SendPasswordResetOtp_ShouldInvalidateOtp_WhenEmailSendFails()
        {
            _repoMock.Setup(r => r.GetByEmail("a@b.com")).Returns(new User());
            _otpMock.Setup(o => o.GenerateOtp("a@b.com")).Returns("1111");
            _emailMock.Setup(e => e.SendOtpEmail("a@b.com", "1111")).Throws(new Exception("smtp fail"));

            Action act = () => _service.SendPasswordResetOtp("a@b.com");
            act.Should().Throw<Exception>().WithMessage("*smtp*");

            _otpMock.Verify(o => o.InvalidateOtp("a@b.com"), Times.Once);
        }
        // ------------------------------------------------------------
        // ResetPassword branches
        // ------------------------------------------------------------
        [Fact]
        public void ResetPassword_ShouldThrow_WhenUserNotFound()
        {
            _otpMock.Setup(o => o.VerifyResetToken("a@b.com", "token")).Returns(true);
            _repoMock.Setup(r => r.GetByEmail("a@b.com")).Returns((User)null);

            Action act = () => _service.ResetPassword("a@b.com", "token", "new");
            act.Should().Throw<InvalidOperationException>().WithMessage("*User not found*");
        }

        [Fact]
        public void ResetPassword_ShouldThrow_WhenTokenInvalid()
        {
            _otpMock.Setup(o => o.VerifyResetToken("a@b.com", "bad")).Returns(false);
            Action act = () => _service.ResetPassword("a@b.com", "bad", "new");
            act.Should().Throw<InvalidOperationException>().WithMessage("*Invalid or expired*");
        }

        // ------------------------------------------------------------
        // ChangePassword branches
        // ------------------------------------------------------------
        [Fact]
        public void ChangePassword_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetById(1)).Returns((User)null);
            Action act = () => _service.ChangePassword(1, "old", "new");
            act.Should().Throw<KeyNotFoundException>().WithMessage("*User not found*");
        }

        [Fact]
        public void ChangePassword_ShouldThrow_WhenCurrentPasswordWrong()
        {
            PasswordService.CreatePasswordHash("right", out var h, out var s);
            var user = new User { UserId = 1, PasswordHash = h, PasswordSalt = s };
            _repoMock.Setup(r => r.GetById(1)).Returns(user);

            Action act = () => _service.ChangePassword(1, "wrong", "new");
            act.Should().Throw<UnauthorizedAccessException>().WithMessage("*incorrect*");
        }

        // ------------------------------------------------------------
        // Restrict / Unrestrict user exception paths
        // ------------------------------------------------------------
        [Fact]
        public void RestrictUser_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetById(5)).Returns((User)null);
            Action act = () => _service.RestrictUser(5);
            act.Should().Throw<KeyNotFoundException>().WithMessage("*User not found*");
        }

        [Fact]
        public void UnrestrictUser_ShouldThrow_WhenUserNotFound()
        {
            _repoMock.Setup(r => r.GetById(6)).Returns((User)null);
            Action act = () => _service.UnrestrictUser(6);
            act.Should().Throw<KeyNotFoundException>().WithMessage("*User not found*");
        }

        // ------------------------------------------------------------
        // GetUserStats branches (null / populated user)
        // ------------------------------------------------------------
        [Fact]
        public void GetUserStats_ShouldReturnNull_WhenUserMissing()
        {
            var result = _service.GetUserStats(999);
            result.Should().BeNull();
        }

        [Fact]
        public void GetUserStats_ShouldReturnPopulatedDto()
        {
            PasswordService.CreatePasswordHash("p", out var h, out var s);

            var u = new User
            {
                UserId = 1,
                Username = "X",
                Email = "x@x.com",
                Role = "user",
                PasswordHash = h,
                PasswordSalt = s,
                Posts = new List<Post>
        {
            new Post { Title = "A", Content = "B", Status = "approved" },
            new Post { Title = "C", Content = "D", Status = "rejected" }
        },
                Comments = new List<Comment>
        {
            new Comment { Content = "First", CreatedAt = DateTime.UtcNow },
            new Comment { Content = "Second", CreatedAt = DateTime.UtcNow }
        },
                CreatedAt = DateTime.UtcNow
            };

            _context.User.Add(u);
            _context.Report.Add(new Report
            {
                Status = "Approved",
                CommentAuthorUserId = 1,
                Content = "Test content" 
            });
            _context.SaveChanges();

            var dto = _service.GetUserStats(1)!;

            dto.TotalPosts.Should().Be(2);
            dto.ApprovedPosts.Should().Be(1);
            dto.RejectedPosts.Should().Be(1);
            dto.ReportedComments.Should().Be(1);
            dto.TotalComments.Should().Be(2);
        }



        // ------------------------------------------------------------
        // Duplicate-email checks in Register / Update
        // ------------------------------------------------------------
        [Fact]
        public void Register_ShouldThrow_InvalidOperation_WhenEmailDuplicate()
        {
            _repoMock.Setup(r => r.GetByEmail("dup@x.com")).Returns(new User { Email = "dup@x.com" });

            var dto = new RegisterRequestDTO { Email = "dup@x.com", Password = "123", Username = "dup" };
            Action act = () => _service.Register(dto);

            act.Should().Throw<InvalidOperationException>().WithMessage("*already been used*");
        }

        [Fact]
        public void Update_ShouldThrow_WhenEmailAlreadyUsedByAnotherUser()
        {
            var admin = new User { UserId = 1, Role = "admin" };
            var target = new User { UserId = 2, Email = "old@x.com" };
            var existing = new User { UserId = 3, Email = "dup@x.com" };

            _repoMock.Setup(r => r.GetById(2)).Returns(target);
            _repoMock.Setup(r => r.GetById(1)).Returns(admin);
            _repoMock.Setup(r => r.GetByEmail("dup@x.com")).Returns(existing);

            var dto = new UpdateUserDTO { Email = "dup@x.com" };
            Action act = () => _service.Update(2, dto, 1);

            act.Should().Throw<InvalidOperationException>().WithMessage("*already been used*");
        }

    
        [Fact]
        public void VerifyOtp_ShouldThrow_WhenExpired()
        {
            _otpMock.Setup(o => o.VerifyOtp("a@b.com", "0000")).Returns(false);
            Action act = () => _service.VerifyOtp("a@b.com", "0000");
            act.Should().Throw<InvalidOperationException>().WithMessage("*Invalid or expired*");
        }

        [Fact]
        public void VerifyOtp_ShouldReturnToken_WhenSuccess()
        {
            _otpMock.Setup(o => o.VerifyOtp("a@b.com", "0000")).Returns(true);
            _otpMock.Setup(o => o.GenerateResetToken("a@b.com")).Returns("token123");

            var token = _service.VerifyOtp("a@b.com", "0000");
            token.Should().Be("token123");
        }
        [Fact]
        public void RegisterAdmin_ShouldThrow_WhenEmailAlreadyUsed()
        {
            // Arrange
            var existing = new User { Email = "admin@x.com" };
            _repoMock.Setup(r => r.GetByEmail("admin@x.com")).Returns(existing);

            var dto = new RegisterRequestDTO
            {
                Email = "admin@x.com",
                Username = "admin1",
                Password = "123"
            };

            // Act
            Action act = () => _service.RegisterAdmin(dto);

            // Assert
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*already been used*");

            _repoMock.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        }
    }
}
