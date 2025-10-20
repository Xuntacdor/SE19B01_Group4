using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace WebAPI.Services
{
    public class OtpService : IOtpService
    {
        private readonly ConcurrentDictionary<string, OtpData> _otpStorage = new();
        private readonly ConcurrentDictionary<string, ResetTokenData> _resetTokenStorage = new();
        private readonly Random _random = new();

        public string GenerateOtp(string email)
        {
            // Generate 6-digit OTP
            var otpCode = _random.Next(100000, 999999).ToString();
            
            // Store OTP in memory with expiration (1 minute)
            var otpData = new OtpData
            {
                OtpCode = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(1),
                CreatedAt = DateTime.UtcNow
            };

            _otpStorage.AddOrUpdate(email, otpData, (key, oldValue) => otpData);
            
            // Clean up expired entries periodically
            CleanupExpiredEntries();
            
            return otpCode;
        }

        public bool VerifyOtp(string email, string otpCode)
        {
            if (!_otpStorage.TryGetValue(email, out var otpData))
                return false;

            // Check if OTP is expired
            if (otpData.ExpiresAt <= DateTime.UtcNow)
            {
                _otpStorage.TryRemove(email, out _);
                return false;
            }

            // Check if OTP code matches
            if (otpData.OtpCode != otpCode)
                return false;

            // OTP is valid, remove it to prevent reuse
            _otpStorage.TryRemove(email, out _);
            return true;
        }

        public string GenerateResetToken(string email)
        {
            // Generate a secure reset token
            var resetToken = Guid.NewGuid().ToString();
            
            // Store reset token in memory with expiration (15 minutes)
            var resetTokenData = new ResetTokenData
            {
                ResetToken = resetToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };

            _resetTokenStorage.AddOrUpdate(email, resetTokenData, (key, oldValue) => resetTokenData);
            
            // Clean up expired entries periodically
            CleanupExpiredEntries();
            
            return resetToken;
        }

        public bool VerifyResetToken(string email, string resetToken)
        {
            if (!_resetTokenStorage.TryGetValue(email, out var tokenData))
                return false;

            // Check if token is expired
            if (tokenData.ExpiresAt <= DateTime.UtcNow)
            {
                _resetTokenStorage.TryRemove(email, out _);
                return false;
            }

            // Check if token matches
            if (tokenData.ResetToken != resetToken)
                return false;

            // Token is valid, remove it to prevent reuse
            _resetTokenStorage.TryRemove(email, out _);
            return true;
        }

        public void InvalidateOtp(string email)
        {
            _otpStorage.TryRemove(email, out _);
            _resetTokenStorage.TryRemove(email, out _);
        }

        private void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            
            // Clean up expired OTPs
            foreach (var kvp in _otpStorage)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _otpStorage.TryRemove(kvp.Key, out _);
                }
            }

            // Clean up expired reset tokens
            foreach (var kvp in _resetTokenStorage)
            {
                if (kvp.Value.ExpiresAt <= now)
                {
                    _resetTokenStorage.TryRemove(kvp.Key, out _);
                }
            }
        }

        private class OtpData
        {
            public string OtpCode { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private class ResetTokenData
        {
            public string ResetToken { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
