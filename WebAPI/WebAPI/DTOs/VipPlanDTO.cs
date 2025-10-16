using System;

namespace WebAPI.DTOs
{
    public class VipPlanDTO
    {
        public int VipPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
