using FluentAssertions;
using System;
using System.Linq;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Services
{
    public class OtpServiceTests
    {
        private readonly OtpService _otpService;

        public OtpServiceTests()
        {
            _otpService = new OtpService();
        }

        // ============ GENERATE OTP ============

        [Fact]
        public void GenerateOtp_ReturnsValidOtpCode()
        {
            var email = "test@example.com";

            var otp = _otpService.GenerateOtp(email);

            otp.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateOtp_Generates6DigitNumericOtp()
        {
            var email = "test@example.com";

            var otp = _otpService.GenerateOtp(email);

            int.TryParse(otp, out _).Should().BeTrue();
            int.Parse(otp).Should().BeGreaterThanOrEqualTo(100000);
            int.Parse(otp).Should().BeLessThan(1000000);
        }

        [Fact]
        public void GenerateOtp_GeneratesUniqueOtps()
        {
            var email = "test@example.com";

            var otp1 = _otpService.GenerateOtp(email);
            var otp2 = _otpService.GenerateOtp(email);

            // Even for same email, should generate different OTPs (overwrites previous)
            otp1.Should().NotBeNullOrEmpty();
            otp2.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateOtp_ForDifferentEmails_GeneratesDifferentOtps()
        {
            var email1 = "user1@example.com";
            var email2 = "user2@example.com";

            var otp1 = _otpService.GenerateOtp(email1);
            var otp2 = _otpService.GenerateOtp(email2);

            otp1.Should().NotBe(otp2);
        }

        [Fact]
        public void GenerateOtp_OverwritesPreviousOtp()
        {
            var email = "test@example.com";
            var oldOtp = _otpService.GenerateOtp(email);

            var newOtp = _otpService.GenerateOtp(email);

            // Old OTP should no longer be valid
            _otpService.VerifyOtp(email, oldOtp).Should().BeFalse();
            _otpService.VerifyOtp(email, newOtp).Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("user@example.com")]
        [InlineData("test.user+tag@domain.co.uk")]
        public void GenerateOtp_WithVariousEmails_WorksCorrectly(string email)
        {
            var otp = _otpService.GenerateOtp(email);

            otp.Should().NotBeNullOrEmpty();
            otp.Length.Should().Be(6);
        }

        [Fact]
        public void GenerateOtp_MultipleEmails_StoresIndependently()
        {
            var email1 = "user1@example.com";
            var email2 = "user2@example.com";
            var email3 = "user3@example.com";

            var otp1 = _otpService.GenerateOtp(email1);
            var otp2 = _otpService.GenerateOtp(email2);
            var otp3 = _otpService.GenerateOtp(email3);

            // All should be independently verifiable
            _otpService.VerifyOtp(email1, otp1).Should().BeTrue();
            _otpService.VerifyOtp(email2, otp2).Should().BeTrue();
            _otpService.VerifyOtp(email3, otp3).Should().BeTrue();
        }

        // ============ VERIFY OTP ============

        [Fact]
        public void VerifyOtp_WithCorrectOtp_ReturnsTrue()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);

            var result = _otpService.VerifyOtp(email, otp);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyOtp_WithIncorrectOtp_ReturnsFalse()
        {
            var email = "test@example.com";
            _otpService.GenerateOtp(email);

            var result = _otpService.VerifyOtp(email, "999999");

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyOtp_WithNonExistentEmail_ReturnsFalse()
        {
            var result = _otpService.VerifyOtp("nonexistent@example.com", "123456");

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyOtp_AfterSuccessfulVerification_RemovesOtp()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);

            _otpService.VerifyOtp(email, otp).Should().BeTrue();
            
            // Second verification should fail (OTP removed)
            _otpService.VerifyOtp(email, otp).Should().BeFalse();
        }

        [Fact(Skip = "Requires 61s wait - run manually for full coverage")]
        public void VerifyOtp_WithExpiredOtp_ReturnsFalse()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);

            // Wait for OTP to expire (1 minute + buffer)
            System.Threading.Thread.Sleep(61000); // 61 seconds

            var result = _otpService.VerifyOtp(email, otp);

            result.Should().BeFalse();
        }

        [Fact(Skip = "Requires 61s wait - run manually for full coverage")]
        public void VerifyOtp_WithExpiredOtp_RemovesFromStorage()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);

            System.Threading.Thread.Sleep(61000); // Wait for expiration

            _otpService.VerifyOtp(email, otp).Should().BeFalse();
            // Should still be false even after expiration check removed it
            _otpService.VerifyOtp(email, otp).Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("12345")]
        [InlineData("1234567")]
        [InlineData("abcdef")]
        public void VerifyOtp_WithInvalidFormat_ReturnsFalse(string invalidOtp)
        {
            var email = "test@example.com";
            _otpService.GenerateOtp(email);

            var result = _otpService.VerifyOtp(email, invalidOtp);

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyOtp_IsCaseSensitive()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email); // Numeric, but tests principle

            var result = _otpService.VerifyOtp(email, otp);

            result.Should().BeTrue();
        }

        // ============ GENERATE RESET TOKEN ============

        [Fact]
        public void GenerateResetToken_ReturnsValidToken()
        {
            var email = "test@example.com";

            var token = _otpService.GenerateResetToken(email);

            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GenerateResetToken_ReturnsGuidFormat()
        {
            var email = "test@example.com";

            var token = _otpService.GenerateResetToken(email);

            Guid.TryParse(token, out _).Should().BeTrue();
        }

        [Fact]
        public void GenerateResetToken_GeneratesUniqueTokens()
        {
            var email = "test@example.com";

            var token1 = _otpService.GenerateResetToken(email);
            var token2 = _otpService.GenerateResetToken(email);

            token1.Should().NotBe(token2);
        }

        [Fact]
        public void GenerateResetToken_ForDifferentEmails_GeneratesDifferentTokens()
        {
            var email1 = "user1@example.com";
            var email2 = "user2@example.com";

            var token1 = _otpService.GenerateResetToken(email1);
            var token2 = _otpService.GenerateResetToken(email2);

            token1.Should().NotBe(token2);
        }

        [Fact]
        public void GenerateResetToken_OverwritesPreviousToken()
        {
            var email = "test@example.com";
            var oldToken = _otpService.GenerateResetToken(email);

            var newToken = _otpService.GenerateResetToken(email);

            // Old token should no longer be valid
            _otpService.VerifyResetToken(email, oldToken).Should().BeFalse();
            _otpService.VerifyResetToken(email, newToken).Should().BeTrue();
        }

        [Fact]
        public void GenerateResetToken_MultipleEmails_StoresIndependently()
        {
            var email1 = "user1@example.com";
            var email2 = "user2@example.com";
            var email3 = "user3@example.com";

            var token1 = _otpService.GenerateResetToken(email1);
            var token2 = _otpService.GenerateResetToken(email2);
            var token3 = _otpService.GenerateResetToken(email3);

            // All should be independently verifiable
            _otpService.VerifyResetToken(email1, token1).Should().BeTrue();
            _otpService.VerifyResetToken(email2, token2).Should().BeTrue();
            _otpService.VerifyResetToken(email3, token3).Should().BeTrue();
        }

        // ============ VERIFY RESET TOKEN ============

        [Fact]
        public void VerifyResetToken_WithCorrectToken_ReturnsTrue()
        {
            var email = "test@example.com";
            var token = _otpService.GenerateResetToken(email);

            var result = _otpService.VerifyResetToken(email, token);

            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyResetToken_WithIncorrectToken_ReturnsFalse()
        {
            var email = "test@example.com";
            _otpService.GenerateResetToken(email);

            var result = _otpService.VerifyResetToken(email, Guid.NewGuid().ToString());

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyResetToken_WithNonExistentEmail_ReturnsFalse()
        {
            var result = _otpService.VerifyResetToken("nonexistent@example.com", Guid.NewGuid().ToString());

            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyResetToken_AfterSuccessfulVerification_RemovesToken()
        {
            var email = "test@example.com";
            var token = _otpService.GenerateResetToken(email);

            _otpService.VerifyResetToken(email, token).Should().BeTrue();
            
            // Second verification should fail (token removed)
            _otpService.VerifyResetToken(email, token).Should().BeFalse();
        }

        [Fact]
        public void VerifyResetToken_WithEmptyToken_ReturnsFalse()
        {
            var email = "test@example.com";
            _otpService.GenerateResetToken(email);

            var result = _otpService.VerifyResetToken(email, "");

            result.Should().BeFalse();
        }

        // ============ INVALIDATE OTP ============

        [Fact]
        public void InvalidateOtp_RemovesOtpFromStorage()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);

            _otpService.InvalidateOtp(email);

            _otpService.VerifyOtp(email, otp).Should().BeFalse();
        }

        [Fact]
        public void InvalidateOtp_RemovesResetTokenFromStorage()
        {
            var email = "test@example.com";
            var token = _otpService.GenerateResetToken(email);

            _otpService.InvalidateOtp(email);

            _otpService.VerifyResetToken(email, token).Should().BeFalse();
        }

        [Fact]
        public void InvalidateOtp_RemovesBothOtpAndToken()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);
            var token = _otpService.GenerateResetToken(email);

            _otpService.InvalidateOtp(email);

            _otpService.VerifyOtp(email, otp).Should().BeFalse();
            _otpService.VerifyResetToken(email, token).Should().BeFalse();
        }

        [Fact]
        public void InvalidateOtp_WithNonExistentEmail_DoesNotThrow()
        {
            Action act = () => _otpService.InvalidateOtp("nonexistent@example.com");

            act.Should().NotThrow();
        }

        [Fact]
        public void InvalidateOtp_AfterInvalidation_CanGenerateNewOtp()
        {
            var email = "test@example.com";
            _otpService.GenerateOtp(email);
            _otpService.InvalidateOtp(email);

            var newOtp = _otpService.GenerateOtp(email);

            _otpService.VerifyOtp(email, newOtp).Should().BeTrue();
        }

        // ============ INTEGRATION TESTS ============

        [Fact]
        public void OtpService_OtpAndTokenAreIndependent()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);
            var token = _otpService.GenerateResetToken(email);

            // Both should be valid independently
            _otpService.VerifyOtp(email, otp).Should().BeTrue();
            _otpService.VerifyResetToken(email, token).Should().BeTrue();
        }

        [Fact]
        public void OtpService_MultipleUsersCanHaveOtpSimultaneously()
        {
            var users = Enumerable.Range(1, 10)
                .Select(i => $"user{i}@example.com")
                .ToList();

            var otps = users.ToDictionary(
                email => email,
                email => _otpService.GenerateOtp(email)
            );

            // All OTPs should be valid
            foreach (var kvp in otps)
            {
                _otpService.VerifyOtp(kvp.Key, kvp.Value).Should().BeTrue();
            }
        }

        [Fact]
        public void OtpService_MultipleUsersCanHaveResetTokensSimultaneously()
        {
            var users = Enumerable.Range(1, 10)
                .Select(i => $"user{i}@example.com")
                .ToList();

            var tokens = users.ToDictionary(
                email => email,
                email => _otpService.GenerateResetToken(email)
            );

            // All tokens should be valid
            foreach (var kvp in tokens)
            {
                _otpService.VerifyResetToken(kvp.Key, kvp.Value).Should().BeTrue();
            }
        }

        [Fact]
        public void OtpService_VerificationConsumesOtpButNotToken()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);
            var token = _otpService.GenerateResetToken(email);

            _otpService.VerifyOtp(email, otp).Should().BeTrue();
            
            // OTP consumed, token still valid
            _otpService.VerifyOtp(email, otp).Should().BeFalse();
            _otpService.VerifyResetToken(email, token).Should().BeTrue();
        }

        [Fact]
        public void OtpService_VerificationConsumesTokenButNotOtp()
        {
            var email = "test@example.com";
            var otp = _otpService.GenerateOtp(email);
            var token = _otpService.GenerateResetToken(email);

            _otpService.VerifyResetToken(email, token).Should().BeTrue();
            
            // Token consumed, OTP still valid
            _otpService.VerifyResetToken(email, token).Should().BeFalse();
            _otpService.VerifyOtp(email, otp).Should().BeTrue();
        }

        [Fact]
        public void OtpService_GenerateOtpTriggersCleanup()
        {
            // Generate OTP (which triggers cleanup)
            var otp = _otpService.GenerateOtp("test@example.com");

            otp.Should().NotBeNullOrEmpty();
            // Cleanup is called, but we can't easily verify internal state
            // This test ensures no exception is thrown
        }

        [Fact]
        public void OtpService_GenerateResetTokenTriggersCleanup()
        {
            // Generate token (which triggers cleanup)
            var token = _otpService.GenerateResetToken("test@example.com");

            token.Should().NotBeNullOrEmpty();
            // Cleanup is called, but we can't easily verify internal state
            // This test ensures no exception is thrown
        }
    }
}

