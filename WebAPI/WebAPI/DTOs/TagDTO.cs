using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class TagDTO
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int PostCount { get; set; }
    }

    public class CreateTagDTO
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string TagName { get; set; } = null!;
    }

    public class UpdateTagDTO
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string TagName { get; set; } = null!;
    }
}
