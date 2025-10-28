namespace WebAPI.Services
{
    public interface ISpeechToTextService
    {
        string TranscribeAndSave(long attemptId, string audioUrl);
    }
}