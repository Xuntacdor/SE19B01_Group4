using WebAPI.Models;
using WebAPI.Repositories;

public class SpeakingFeedbackService : ISpeakingFeedbackService
{
    private readonly ISpeakingFeedbackRepository _repo;

    public SpeakingFeedbackService(ISpeakingFeedbackRepository repo)
    {
        _repo = repo;
    }

    public List<SpeakingFeedback> GetByExamAndUser(int examId, int userId)
        => _repo.GetByExamAndUser(examId, userId);

    public SpeakingFeedback? GetBySpeakingAndUser(int speakingId, int userId)
        => _repo.GetBySpeakingAndUser(speakingId, userId);

    public void Add(SpeakingFeedback feedback)
    {
        _repo.Add(feedback);
        _repo.SaveChanges();
    }

    public void Update(SpeakingFeedback feedback)
    {
        _repo.Update(feedback);
        _repo.SaveChanges();
    }

    public void Delete(SpeakingFeedback feedback)
    {
        _repo.Delete(feedback);
        _repo.SaveChanges();
    }
}
