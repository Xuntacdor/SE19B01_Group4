using System.Text.Json;

namespace WebAPI.ExternalServices
{
    public interface IOpenAIService
    {
        JsonDocument GradeWriting(string question, string answer, string? imageUrl = null);
        string SpeechToText(string audioUrl);
        JsonDocument GradeSpeaking(string question, string transcript);
    }
}
