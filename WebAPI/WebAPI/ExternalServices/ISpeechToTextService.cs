using System;

namespace WebAPI.Services
{
   
    public interface ISpeechToTextService
    {
      
        string TranscribeAndSave(long attemptId, string audioUrl, string language = "en");

      
        bool TestCloudinaryAccess(string audioUrl);
        string TranscribeFromFile(string filePath, long attemptId, string language = "en");
    }
}
