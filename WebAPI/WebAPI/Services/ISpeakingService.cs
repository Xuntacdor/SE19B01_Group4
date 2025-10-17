using System.Collections.Generic;
using System.Text.Json;
using WebAPI.DTOs;

namespace WebAPI.Services
{
    public interface ISpeakingService
    {
        SpeakingDTO? GetById(int id);
        List<SpeakingDTO> GetByExam(int examId);
        SpeakingDTO Create(SpeakingDTO dto);
        SpeakingDTO? Update(int id, SpeakingDTO dto);
        bool Delete(int id);
        JsonDocument GradeSpeaking(SpeakingGradeRequestDTO dto, int userId);
    }
}
