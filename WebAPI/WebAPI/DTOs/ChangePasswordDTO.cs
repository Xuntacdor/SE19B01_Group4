using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class ChangePasswordDTO
    {
        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

