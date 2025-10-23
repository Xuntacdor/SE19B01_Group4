using Microsoft.Extensions.Logging;
using WebAPI.DTOs;
using WebAPI.Repositories;
using WebAPI.Models;

namespace WebAPI.Services.Authorization
{
    public class VipAuthorizationService : IVipAuthorizationService
    {
        private readonly IUserRepository _userRepo;
        private readonly ILogger<VipAuthorizationService> _logger;

        public VipAuthorizationService(IUserRepository userRepo, ILogger<VipAuthorizationService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public bool IsUserVip(int userId)
        {
            var user = _userRepo.GetById(userId);
            if (user == null)
            {
                _logger.LogWarning($"[VIP] User {userId} not found.");
                return false;
            }

            return user.VipExpireAt != null && user.VipExpireAt > DateTime.UtcNow;
        }

        public DateTime GetVipExpireDate(int userId)
        {
            var user = _userRepo.GetById(userId);
            if (user == null || user.VipExpireAt == null)
                return DateTime.MinValue;

            return user.VipExpireAt.Value;
        }

        public void EnsureVipAccess(int userId, string feature)
        {
            if (!IsUserVip(userId))
            {
                _logger.LogInformation($"[VIP] Access denied for user {userId} on feature {feature}");
                throw new UnauthorizedAccessException("You must be a VIP to access this feature.");
            }
        }

        public VipStatusDTO GetVipStatus(int userId)
        {
            var user = _userRepo.GetById(userId);
            if (user == null)
            {
                return new VipStatusDTO
                {
                    IsVip = false,
                    Message = "User not found"
                };
            }

            if (user.VipExpireAt == null || user.VipExpireAt <= DateTime.UtcNow)
            {
                return new VipStatusDTO
                {
                    IsVip = false,
                    ExpiresAt = user.VipExpireAt,
                    Message = "Your VIP membership has expired."
                };
            }

            var remaining = (user.VipExpireAt.Value - DateTime.UtcNow).Days;
            return new VipStatusDTO
            {
                IsVip = true,
                ExpiresAt = user.VipExpireAt.Value,
                DaysRemaining = remaining,
                Message = $"Your VIP is active. {remaining} day(s) remaining."
            };
        }
    }
}
