namespace WebAPI.DTOs
{
    public class SpeechTranscribeDto
    {
        public long AttemptId { get; set; }
        public string AudioUrl { get; set; } = string.Empty;
    }
}