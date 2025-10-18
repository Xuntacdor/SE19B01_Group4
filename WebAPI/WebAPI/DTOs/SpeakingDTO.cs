using System;
using System.Collections.Generic;

namespace WebAPI.DTOs
{
    // === CƠ BẢN CHO CRUD ===
    public class SpeakingDTO
    {
        public int SpeakingId { get; set; }
        public int ExamId { get; set; }
        public string SpeakingQuestion { get; set; }
        public string SpeakingType { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // === DÙNG KHI NGƯỜI DÙNG GỬI AUDIO/TRANSCRIPT ===
    public class SpeakingAnswerDTO
    {
        public int SpeakingId { get; set; }
        public int DisplayOrder { get; set; }

        // URL của audio đã upload (FE gửi lên Cloudinary)
        public string AudioUrl { get; set; }

        // FE có thể gửi transcript text (sau khi TTS xử lý)
        public string Transcript { get; set; }
    }

    // === REQUEST CHẤM ĐIỂM ===
    public class SpeakingGradeRequestDTO
    {
        public int ExamId { get; set; }
        public int UserId { get; set; }
        public string Mode { get; set; } // "full" hoặc "single"
        public List<SpeakingAnswerDTO> Answers { get; set; }
    }

    // === KẾT QUẢ CHẤM AI TRẢ VỀ ===
    public class SpeakingFeedbackDTO
    {
        public int AttemptId { get; set; }
        public int SpeakingId { get; set; }

        // Thang điểm 0-9
        public decimal Pronunciation { get; set; }
        public decimal Fluency { get; set; }
        public decimal Coherence { get; set; }
        public decimal LexicalResource { get; set; }
        public decimal GrammarRange { get; set; }
        public decimal Overall { get; set; }

        // Lưu JSON của chi tiết chấm (AI feedback text)
        public string AiFeedbackJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
