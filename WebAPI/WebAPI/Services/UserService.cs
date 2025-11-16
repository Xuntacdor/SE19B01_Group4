using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.ExternalServices;

namespace WebAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IOtpService _otpService;
        
        public UserService(IUserRepository repo, IEmailService emailService, ApplicationDbContext context, IOtpService otpService)
        {
            _repo = repo;
            _emailService = emailService;
            _context = context;
            _otpService = otpService;
        }

        public User? GetById(int id) => _repo.GetById(id);
        public User? GetByEmail(string email) => _repo.GetByEmail(email);
        public IEnumerable<User> GetAll() => _repo.GetAll();
        public bool Exists(int userId) => _repo.Exists(userId);

        public User Register(RegisterRequestDTO dto)
        {
            if (_repo.GetByEmail(dto.Email) != null)
                throw new InvalidOperationException("Email has already been used");

            PasswordService.CreatePasswordHash(dto.Password, out var hash, out var salt);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Firstname = dto.Firstname,
                Lastname = dto.Lastname,
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                Avatar = "https://res.cloudinary.com/dutxxv0ow/image/upload/v1762135195/ieltsphobic/images/blob_emodva.png"
            };

            _repo.Add(user);
            _repo.SaveChanges();
            return user;
        }

        public User RegisterAdmin(RegisterRequestDTO dto)
        {
            if (_repo.GetByEmail(dto.Email) != null)
                throw new InvalidOperationException("Email has already been used");

            PasswordService.CreatePasswordHash(dto.Password, out var hash, out var salt);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hash,
                PasswordSalt = salt,
                Firstname = dto.Firstname,
                Lastname = dto.Lastname,
                Role = "admin",
                CreatedAt = DateTime.UtcNow
            };

            _repo.Add(user);
            _repo.SaveChanges();
            return user;
        }

        public User? Authenticate(string email, string password)
        {
            var user = _repo.GetByEmail(email);
            if (user == null)
                throw new UnauthorizedAccessException("Account not found with this email address");
            
            if (!PasswordService.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedAccessException("Incorrect password");

            return user;
        }

        public void Update(int id, UpdateUserDTO dto, int currentUserId)
        {
            var user = _repo.GetById(id) ?? throw new KeyNotFoundException("User not found");
            var currentUser = _repo.GetById(currentUserId) ?? throw new UnauthorizedAccessException("Current user not found");

            if (currentUser.UserId != id && currentUser.Role != "admin")
                throw new UnauthorizedAccessException("You do not have permission to update this user");

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var existing = _repo.GetByEmail(dto.Email);
                if (existing != null && existing.UserId != id)
                    throw new InvalidOperationException("Email has already been used");
                user.Email = dto.Email;
            }

            if (!string.IsNullOrWhiteSpace(dto.Firstname)) user.Firstname = dto.Firstname;
            if (!string.IsNullOrWhiteSpace(dto.Lastname)) user.Lastname = dto.Lastname;
            if (!string.IsNullOrWhiteSpace(dto.Username)) user.Username = dto.Username;
            user.Avatar = string.IsNullOrWhiteSpace(dto.Avatar) ? null : dto.Avatar;

            if (!string.IsNullOrWhiteSpace(dto.Role) && currentUser.Role == "admin")
                user.Role = dto.Role;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                PasswordService.CreatePasswordHash(dto.Password, out var hash, out var salt);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
            }

            user.UpdatedAt = DateTime.UtcNow;
            _repo.Update(user);
            _repo.SaveChanges();
        }

        public void Update(User user)
        {
            _repo.Update(user);
            _repo.SaveChanges();
        }

        public void Delete(int id, int currentUserId)
        {
            var user = _repo.GetById(id) ?? throw new KeyNotFoundException("User not found");
            var currentUser = _repo.GetById(currentUserId) ?? throw new UnauthorizedAccessException("Current user not found");

            if (currentUser.UserId != id && currentUser.Role != "admin")
                throw new UnauthorizedAccessException("You do not have permission to delete this user");

            _repo.Delete(user);
            _repo.SaveChanges();
        }

        public string SendPasswordResetOtp(string email)
        {
            var user = _repo.GetByEmail(email);
            if (user == null)
            {
                throw new InvalidOperationException("No account found with this email address");
            }

            // Generate OTP using the secure in-memory service
            var otpCode = _otpService.GenerateOtp(email);

            // Send email
            try
            {
                _emailService.SendOtpEmail(email, otpCode);
            }
            catch (Exception ex)
            {
                // Invalidate OTP if email sending fails
                _otpService.InvalidateOtp(email);
                throw;
            }

            return "OTP sent successfully to your email address";
        }

        public string VerifyOtp(string email, string otpCode)
        {
            // Verify OTP using the secure in-memory service
            if (!_otpService.VerifyOtp(email, otpCode))
                throw new InvalidOperationException("Invalid or expired OTP");

            // Generate a secure reset token
            var resetToken = _otpService.GenerateResetToken(email);
            
            return resetToken;
        }

        public string ResetPassword(string email, string resetToken, string newPassword)
        {
            // Verify reset token using the secure in-memory service
            if (!_otpService.VerifyResetToken(email, resetToken))
                throw new InvalidOperationException("Invalid or expired reset token");

            // Get user
            var user = _repo.GetByEmail(email);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Update password
            PasswordService.CreatePasswordHash(newPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTime.UtcNow;

            _repo.Update(user);
            _repo.SaveChanges();

            return "Password reset successfully";
        }

        public string ChangePassword(int userId, string currentPassword, string newPassword)
        {
            // Get user
            var user = _repo.GetById(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            // Verify current password
            if (!PasswordService.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
                throw new UnauthorizedAccessException("Current password is incorrect");

            // Update to new password
            PasswordService.CreatePasswordHash(newPassword, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTime.UtcNow;

            _repo.Update(user);
            _repo.SaveChanges();

            return "Password changed successfully";
        }

        // Moderator methods
        public IEnumerable<UserStatsDTO> GetUsersWithStats(int page, int limit)
        {
            var users = _context.User
                .Include(u => u.Posts)
                .Include(u => u.Comments)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return users.Select(user => new UserStatsDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                TotalPosts = user.Posts?.Count ?? 0,
                TotalComments = user.Comments?.Count ?? 0,
                ApprovedPosts = user.Posts?.Count(p => p.Status == "approved") ?? 0,
                RejectedPosts = user.Posts?.Count(p => p.Status == "rejected") ?? 0,
                // Count only "Approved" reports (not "Resolved") to avoid double-counting when multiple users report the same comment
                ReportedComments = _context.Report.Count(r => r.Status == "Approved" && r.CommentAuthorUserId == user.UserId),
                CreatedAt = user.CreatedAt,
                IsRestricted = user.IsRestricted
            });
        }

        public UserStatsDTO? GetUserStats(int userId)
        {
            var user = _context.User
                .Include(u => u.Posts)
                .Include(u => u.Comments)
                .FirstOrDefault(u => u.UserId == userId);
            
            if (user == null) return null;

            return new UserStatsDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                TotalPosts = user.Posts?.Count ?? 0,
                TotalComments = user.Comments?.Count ?? 0,
                ApprovedPosts = user.Posts?.Count(p => p.Status == "approved") ?? 0,
                RejectedPosts = user.Posts?.Count(p => p.Status == "rejected") ?? 0,
                // Count only "Approved" reports (not "Resolved") to avoid double-counting when multiple users report the same comment
                ReportedComments = _context.Report.Count(r => r.Status == "Approved" && r.CommentAuthorUserId == userId),
                CreatedAt = user.CreatedAt,
                IsRestricted = user.IsRestricted
            };
        }

        public UserProfileStatsDTO? GetUserProfileStats(int userId)
        {
            var user = _context.User
                .Include(u => u.Posts)
                .ThenInclude(p => p.PostLikes)
                .FirstOrDefault(u => u.UserId == userId);
            
            if (user == null) return null;

            var totalVotes = user.Posts?.Sum(p => p.PostLikes?.Count ?? 0) ?? 0;

            return new UserProfileStatsDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Avatar = user.Avatar,
                TotalPosts = user.Posts?.Count ?? 0,
                TotalVotes = totalVotes
            };
        }
        public bool IsVip(int userId)
        {
            var user = _repo.GetById(userId);
            return user?.VipExpireAt != null && user.VipExpireAt > DateTime.UtcNow;
        }

        public void RestrictUser(int userId)
        {
            var user = _repo.GetById(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");
            
            user.IsRestricted = true;
            user.UpdatedAt = DateTime.UtcNow;
            _repo.Update(user);
            _repo.SaveChanges();
            
            // Send notification to user
            var notification = new Notification
            {
                UserId = userId,
                Content = "Your account has been restricted from posting and commenting on the forum due to multiple violations of community guidelines. Please contact support if you believe this is an error.",
                Type = "account_restricted",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notification.Add(notification);
            _context.SaveChanges();
        }

        public void UnrestrictUser(int userId)
        {
            var user = _repo.GetById(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");
            
            user.IsRestricted = false;
            user.UpdatedAt = DateTime.UtcNow;
            _repo.Update(user);
            _repo.SaveChanges();
            
            // Send notification to user
            var notification = new Notification
            {
                UserId = userId,
                Content = "Your account restriction has been lifted. You can now post and comment on the forum again. Please follow our community guidelines to avoid future restrictions.",
                Type = "account_unrestricted",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notification.Add(notification);
            _context.SaveChanges();
        }

    }
}
