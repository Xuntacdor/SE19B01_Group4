using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class AudioTranscribeDto
    {
        [Required]
        public IFormFile AudioFile { get; set; } = null!;
        
        [Required]
        public long AttemptId { get; set; }
    }
}
