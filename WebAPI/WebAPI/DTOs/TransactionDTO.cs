using System;

namespace WebAPI.DTOs
{
    /// <summary>
    /// Dùng chung cho tất cả thao tác liên quan đến Transaction
    /// </summary>
    public sealed class TransactionDTO
    {
        // --- Phần chung (dùng cho mọi response) ---
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string? PaymentMethod { get; set; }
        public string? ProviderTxnId { get; set; }
        public string Purpose { get; set; } = "VIP";
        public string Status { get; set; } = "PENDING";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Dành cho tạo mới (CreateTransactionDTO cũ) ---
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
        public int PlanId { get; set; }

        // --- Dành cho filter (TransactionQueryDTO cũ) ---
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "createdAt"; // createdAt|amount|status
        public string SortDir { get; set; } = "desc";     // asc|desc
        public string? FilterStatus { get; set; }
        public string? FilterType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string? Search { get; set; }

        // --- Factory helpers ---
        public static TransactionDTO FromCreate(decimal amount, string purpose, string reference)
        {
            return new TransactionDTO
            {
                Amount = amount,
                Purpose = purpose,
                ProviderTxnId = reference,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
