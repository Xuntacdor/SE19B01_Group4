using System;

namespace WebAPI.Services
{
    public interface IOtpService
    {
        string GenerateOtp(string email);
        bool VerifyOtp(string email, string otpCode);
        string GenerateResetToken(string email);
        bool VerifyResetToken(string email, string resetToken);
        void InvalidateOtp(string email);
    }
}
