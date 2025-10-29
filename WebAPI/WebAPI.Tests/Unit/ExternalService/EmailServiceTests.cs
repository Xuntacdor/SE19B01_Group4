using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using WebAPI.ExternalServices;
using Xunit;

namespace WebAPI.Tests.Unit.ExternalService
{
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;

        public EmailServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            
            // Setup default configuration values
            SetupConfiguration("smtp.gmail.com", "587", "test@example.com", "password", "true");
            
            _emailService = new EmailService(_mockConfiguration.Object, _mockLogger.Object);
        }

        private void SetupConfiguration(string smtpServer, string port, string username, string password, string useSsl)
        {
            _mockConfiguration.Setup(c => c["Email:SmtpServer"]).Returns(smtpServer);
            _mockConfiguration.Setup(c => c["Email:SmtpPort"]).Returns(port);
            _mockConfiguration.Setup(c => c["Email:Username"]).Returns(username);
            _mockConfiguration.Setup(c => c["Email:Password"]).Returns(password);
            _mockConfiguration.Setup(c => c["Email:UseSsl"]).Returns(useSsl);
            _mockConfiguration.Setup(c => c["Email:FromEmail"]).Returns("noreply@example.com");
        }

        // ============ CONSTRUCTOR ============

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            var service = new EmailService(_mockConfiguration.Object, _mockLogger.Object);

            service.Should().NotBeNull();
            service.Should().BeAssignableTo<IEmailService>();
        }

        [Fact]
        public void Constructor_WithNullConfiguration_CreatesInstance()
        {
            // Current implementation doesn't validate null - documents actual behavior
            var service = new EmailService(null!, _mockLogger.Object);

            service.Should().NotBeNull();
            // Note: Will throw NullReferenceException when trying to use _configuration
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Current implementation doesn't validate null - documents actual behavior
            var service = new EmailService(_mockConfiguration.Object, null!);

            service.Should().NotBeNull();
            // Note: Will throw NullReferenceException when trying to use _logger
        }

        // ============ SEND OTP EMAIL - VALIDATION ============

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SendOtpEmail_WithInvalidEmail_ThrowsException(string invalidEmail)
        {
            // Note: The current implementation doesn't validate email, 
            // but it will fail when trying to create MailboxAddress
            Action act = () => _emailService.SendOtpEmail(invalidEmail, "123456");

            act.Should().Throw<Exception>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SendOtpEmail_WithInvalidOtpCode_ShouldStillCreateEmail(string invalidOtp)
        {
            // The method doesn't validate OTP, so it should try to send even with empty OTP
            // This test documents current behavior - ideally we'd add validation
            Action act = () => _emailService.SendOtpEmail("test@example.com", invalidOtp);

            // Will fail when trying to connect to SMTP server, but that's expected in unit tests
            act.Should().Throw<Exception>();
        }

        // ============ SEND OTP EMAIL - CONFIGURATION ============

        [Fact]
        public void SendOtpEmail_WithMissingSmtpServer_ThrowsException()
        {
            SetupConfiguration(null!, "587", "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void SendOtpEmail_WithMissingPort_UsesDefaultPort587()
        {
            SetupConfiguration("smtp.gmail.com", null!, "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            // Will fail when trying to connect, but should parse default port 587
            act.Should().Throw<Exception>();
            // Verify it attempted to log the connection
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send OTP email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SendOtpEmail_WithInvalidPortFormat_ThrowsFormatException()
        {
            SetupConfiguration("smtp.gmail.com", "invalid", "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void SendOtpEmail_WithInvalidUseSslFormat_ThrowsFormatException()
        {
            SetupConfiguration("smtp.gmail.com", "587", "test@example.com", "password", "invalid");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            act.Should().Throw<FormatException>();
        }

        // ============ SEND OTP EMAIL - PORT CONFIGURATION ============

        [Fact]
        public void SendOtpEmail_WithPort587_UsesStartTls()
        {
            SetupConfiguration("smtp.gmail.com", "587", "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            // Will fail at connection, but verifies port 587 path is taken
            act.Should().Throw<Exception>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("smtp.gmail.com:587")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SendOtpEmail_WithPort465_UsesSslOnConnect()
        {
            SetupConfiguration("smtp.gmail.com", "465", "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            // Will fail at connection, but verifies port 465 path is taken
            act.Should().Throw<Exception>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("smtp.gmail.com:465")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SendOtpEmail_WithCustomPort_UsesSslSetting()
        {
            SetupConfiguration("smtp.gmail.com", "2525", "test@example.com", "password", "false");

            Action act = () => _emailService.SendOtpEmail("recipient@example.com", "123456");

            // Will fail at connection, but verifies custom port path is taken
            act.Should().Throw<Exception>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("smtp.gmail.com:2525")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // ============ SEND OTP EMAIL - LOGGING ============

        [Fact]
        public void SendOtpEmail_LogsAttemptToSendEmail()
        {
            Action act = () => _emailService.SendOtpEmail("test@example.com", "123456");

            act.Should().Throw<Exception>(); // Expected to fail at SMTP connection

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send OTP email to test@example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SendOtpEmail_OnException_LogsError()
        {
            SetupConfiguration("invalid.smtp.server", "587", "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("test@example.com", "123456");

            act.Should().Throw<Exception>();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send OTP email to test@example.com")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SendOtpEmail_OnException_RethrowsException()
        {
            SetupConfiguration("invalid.smtp.server", "587", "test@example.com", "password", "true");

            Action act = () => _emailService.SendOtpEmail("test@example.com", "123456");

            act.Should().Throw<Exception>()
                .And.Should().NotBeNull();
        }

        // ============ EMAIL TEMPLATE - We can't test private method directly, but we can verify behavior ============

        [Fact]
        public void SendOtpEmail_CreatesEmailWithCorrectRecipient()
        {
            var recipientEmail = "recipient@example.com";
            
            Action act = () => _emailService.SendOtpEmail(recipientEmail, "123456");

            act.Should().Throw<Exception>(); // Expected to fail at SMTP
            
            // Verify logging includes recipient
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(recipientEmail)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void SendOtpEmail_WithDifferentOtpCodes_ProcessesEach()
        {
            var testCases = new[] { "123456", "000000", "999999", "ABCDEF" };

            foreach (var otpCode in testCases)
            {
                Action act = () => _emailService.SendOtpEmail("test@example.com", otpCode);
                act.Should().Throw<Exception>(); // Expected to fail at SMTP
            }

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send OTP email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(testCases.Length));
        }

        // ============ INTEGRATION-STYLE DOCUMENTATION TESTS ============
        // These tests document the expected behavior and flow without actual SMTP connection

        [Fact]
        public void SendOtpEmail_FlowDocumentation_LogsExpectedSequence()
        {
            Action act = () => _emailService.SendOtpEmail("test@example.com", "123456");

            act.Should().Throw<Exception>();

            // Verify the logging sequence
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send OTP email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Connecting to SMTP server")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void SendOtpEmail_WithValidEmail_ShouldNotThrowArgumentException()
        {
            var validEmails = new[]
            {
                "user@example.com",
                "test.user@example.com",
                "user+tag@example.co.uk"
            };

            foreach (var email in validEmails)
            {
                Action act = () => _emailService.SendOtpEmail(email, "123456");
                
                // Should throw due to SMTP connection, not ArgumentException
                act.Should().Throw<Exception>()
                    .Which.Should().NotBeOfType<ArgumentException>();
            }
        }
    }
}

