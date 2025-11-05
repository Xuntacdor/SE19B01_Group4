using System.Text.Json;

namespace WebAPI.ExternalServices
{
    public interface IOpenAIService
    {
        JsonDocument GradeWriting(string question, string answer, string? imageUrl = null);
        JsonDocument GradeSpeaking(string question, string transcript);
    }
}
