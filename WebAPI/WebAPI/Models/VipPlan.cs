using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models
{
    public class VipPlan
    {
        [Column("plan_id")] public int VipPlanId { get; set; }
        [Column("plan_name")] public string PlanName { get; set; } = string.Empty;
        [Column("duration_days")] public int DurationDays { get; set; }
        [Column("price")] public decimal Price { get; set; }
        [Column("description")] public string? Description { get; set; }
        [Column("created_at")] public DateTime CreatedAt { get; set; }

    }
}
