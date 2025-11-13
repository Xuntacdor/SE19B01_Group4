using WebAPI.Models;

public interface ISpeakingFeedbackService
{
    List<SpeakingFeedback> GetByExamAndUser(int examId, int userId);
    SpeakingFeedback? GetBySpeakingAndUser(int speakingId, int userId);
    SpeakingFeedback GetBySpeakingAttemptAndUser(int speakingAttemptId, int userId);

    void Add(SpeakingFeedback feedback);
    void Update(SpeakingFeedback feedback);
    void Delete(SpeakingFeedback feedback);
}
