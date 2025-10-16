using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAPI.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string? ProviderTxnId { get; set; }

    public string Purpose { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    [Column("plan_id")]
    public int? PlanId { get; set; }
    public VipPlan? Plan { get; set; }
    public virtual User User { get; set; } = null!;
}
