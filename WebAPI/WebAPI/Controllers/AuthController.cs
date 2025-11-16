using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.ExternalServices;
using WebAPI.Models;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ISignInHistoryService _signInHistoryService;

        public AuthController(IUserService userService, IConfiguration configuration, IEmailService emailService, ISignInHistoryService signInHistoryService)
        {
            _userService = userService;
            _configuration = configuration;
            _emailService = emailService;
            _signInHistoryService = signInHistoryService;
        }

        [HttpPost("register")]
        public ActionResult<UserDTO> Register([FromBody] RegisterRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var user = _userService.Register(dto);
                HttpContext.Session.SetInt32("UserId", user.UserId);
                return Created("", ToDto(user));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("login")]
        public ActionResult<UserDTO> Login([FromBody] LoginRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var user = _userService.Authenticate(dto.Email, dto.Password);

                HttpContext.Session.SetInt32("UserId", user.UserId);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim("UserId", user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                    }).GetAwaiter().GetResult();

                try
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var device = Request.Headers["User-Agent"].ToString();
                    _signInHistoryService.LogSignIn(user.UserId, ip, device);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AuthController] Failed to log sign-in: {ex.Message}");
                }

                return Ok(ToDto(user));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new AuthResponse { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse { message = "An error occurred during login" });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                       .GetAwaiter().GetResult();
            HttpContext.Session.Clear();
            return Ok("Logged out successfully");
        }

        [HttpGet("me")]
        public ActionResult<UserDTO> Me()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (claimId != null && int.TryParse(claimId, out int id))
                    userId = id;
            }

            if (userId == null) return Unauthorized();

            var user = _userService.GetById(userId.Value);
            if (user == null) return Unauthorized();

            return Ok(ToDto(user));
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public ActionResult ForgotPassword([FromBody] ForgotPasswordRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = _userService.SendPasswordResetOtp(dto.Email);
                return Ok(new { message = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while sending OTP" });
            }
        }

        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public ActionResult VerifyOtp([FromBody] VerifyOtpRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var resetToken = _userService.VerifyOtp(dto.Email, dto.OtpCode);
                return Ok(new { message = "OTP verified successfully", resetToken = resetToken });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while verifying OTP" });
            }
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public ActionResult ResetPassword([FromBody] ResetPasswordRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = _userService.ResetPassword(dto.Email, dto.ResetToken, dto.NewPassword);
                return Ok(new { message = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while resetting password" });
            }
        }

        [HttpPost("change-password")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public ActionResult ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized("Not logged in");

            try
            {
                var result = _userService.ChangePassword(userId.Value, dto.CurrentPassword, dto.NewPassword);
                return Ok(new { message = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while changing password" });
            }
        }

        [HttpPost("test-email")]
        public ActionResult TestEmail([FromBody] ForgotPasswordRequestDTO dto)
        {
            try
            {
                Console.WriteLine($"[EMAIL TEST] Testing email configuration...");
                Console.WriteLine($"[EMAIL TEST] SMTP Server: {_configuration["Email:SmtpServer"] ?? "Not configured"}");
                Console.WriteLine($"[EMAIL TEST] SMTP Port: {_configuration["Email:SmtpPort"] ?? "Not configured"}");
                Console.WriteLine($"[EMAIL TEST] Username: {_configuration["Email:Username"] ?? "Not configured"}");
                Console.WriteLine($"[EMAIL TEST] FromEmail: {_configuration["Email:FromEmail"] ?? "Not configured"}");

                // Test email sending
                _emailService.SendOtpEmail(dto.Email, "123456");

                return Ok(new { message = "Email test successful", email = dto.Email });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL TEST] Error: {ex.Message}");
                Console.WriteLine($"[EMAIL TEST] Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Email test failed", error = ex.Message });
            }
        }

        private static UserDTO ToDto(User user) => new UserDTO
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            Role = user.Role,
            Avatar = user.Avatar
        };
    }
}
