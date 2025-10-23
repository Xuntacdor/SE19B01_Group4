using System;

namespace WebAPI.DTOs
{
    public class VipStatusDTO
    {
        
        public bool IsVip { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int DaysRemaining { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
