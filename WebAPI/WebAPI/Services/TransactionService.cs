using System;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using System.Linq.Expressions;

namespace WebAPI.Services
{
    public sealed class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _db;
        private const int CsvMaxRows = 50000;

        public TransactionService(ApplicationDbContext db)
        {
            _db = db;
        }

        // =====================
        // == GET Paged ==
        // =====================
        public PagedResult<TransactionDTO> GetPaged(TransactionDTO q, int currentUserId, bool isAdmin)
        {
            var scoped = ScopeQuery(q, currentUserId, isAdmin);
            var filtered = ApplyFilters(scoped, q);
            var sorted = ApplySort(filtered, q)
                .Include(t => t.User)
                .AsNoTracking();

            var total = sorted.Count();
            var items = sorted
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(MapToDtoExpr())
                .ToList();

            return new PagedResult<TransactionDTO>
            {
                Items = items,
                Total = total,
                Page = q.Page,
                PageSize = q.PageSize
            };
        }

        // =====================
        // == GET By Id ==
        // =====================
        public TransactionDTO? GetById(int id, int currentUserId, bool isAdmin)
        {
            var tx = _db.Transactions
                .Include(t => t.User)
                .AsNoTracking()
                .FirstOrDefault(t => t.TransactionId == id);

            if (tx == null) return null;
            if (!isAdmin && tx.UserId != currentUserId) return null;
            return MapToDto(tx);
        }

        // =====================
        // == CREATE ==
        // =====================
        public TransactionDTO CreateOrGetByReference(TransactionDTO dto, int currentUserId)
        {
            var reference = dto.ProviderTxnId ?? dto.ReferenceCode;
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("ReferenceCode is required.");

            var existing = _db.Transactions
                .AsNoTracking()
                .FirstOrDefault(t => t.ProviderTxnId == reference);

            if (existing != null) return MapToDto(existing);

            var entity = new Transaction
            {
                UserId = currentUserId,
                Amount = dto.Amount,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "VND" : dto.Currency,
                PaymentMethod = dto.PaymentMethod,
                ProviderTxnId = reference,
                Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? "VIP" : dto.Purpose,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(entity);
            _db.SaveChanges();

            return MapToDto(entity);
        }

        // =====================
        // == CANCEL ==
        // =====================
        public TransactionDTO Cancel(int id, int currentUserId, bool isAdmin)
        {
            var tx = _db.Transactions.FirstOrDefault(t => t.TransactionId == id);
            if (tx == null) throw new KeyNotFoundException("Transaction not found.");
            if (!isAdmin && tx.UserId != currentUserId)
                throw new UnauthorizedAccessException("Forbidden.");

            if (!tx.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only pending transactions can be canceled.");

            tx.Status = "FAILED";
            _db.SaveChanges();
            return MapToDto(tx);
        }

        // =====================
        // == REFUND ==
        // =====================
        public TransactionDTO Refund(int id, int currentUserId, bool isAdmin)
        {
            var tx = _db.Transactions.FirstOrDefault(t => t.TransactionId == id);
            if (tx == null) throw new KeyNotFoundException("Transaction not found.");
            if (!isAdmin && tx.UserId != currentUserId)
                throw new UnauthorizedAccessException("Forbidden.");

            if (!tx.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only paid transactions can be refunded.");

            tx.Status = "REFUNDED";
            _db.SaveChanges();
            return MapToDto(tx);
        }

        // =====================
        // == APPROVE ==
        // =====================
        public TransactionDTO Approve(int id, int currentUserId, bool isAdmin)
        {
            if (!isAdmin)
                throw new UnauthorizedAccessException("Only admin can approve.");

            var tx = _db.Transactions.FirstOrDefault(t => t.TransactionId == id);
            if (tx == null) throw new KeyNotFoundException("Transaction not found.");

            if (!tx.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only pending transactions can be approved.");

            tx.Status = "PAID";
            _db.SaveChanges();
            return MapToDto(tx);
        }

        // =====================
        // == EXPORT CSV ==
        // =====================
        public byte[] ExportCsv(TransactionDTO q, int currentUserId, bool isAdmin)
        {
            var scoped = ScopeQuery(q, currentUserId, isAdmin);
            var filtered = ApplyFilters(scoped, q);
            var sorted = ApplySort(filtered, q).AsNoTracking();

            var rows = sorted
                .Include(t => t.User)
                .Take(CsvMaxRows)
                .Select(t => new
                {
                    t.TransactionId,
                    t.UserId,
                    Username = t.User.Username,
                    t.Amount,
                    t.Currency,
                    t.PaymentMethod,
                    t.ProviderTxnId,
                    t.Purpose,
                    t.Status,
                    t.CreatedAt
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("TransactionId,UserId,Username,Amount,Currency,PaymentMethod,ReferenceCode,Purpose,Status,CreatedAt");

            foreach (var r in rows)
            {
                sb.Append(r.TransactionId).Append(',')
                  .Append(r.UserId).Append(',')
                  .Append(EscapeCsv(r.Username)).Append(',')
                  .Append(r.Amount.ToString("0.00")).Append(',')
                  .Append(r.Currency).Append(',')
                  .Append(EscapeCsv(r.PaymentMethod)).Append(',')
                  .Append(EscapeCsv(r.ProviderTxnId)).Append(',')
                  .Append(EscapeCsv(r.Purpose)).Append(',')
                  .Append(r.Status).Append(',')
                  .Append(r.CreatedAt.ToString("O"))
                  .AppendLine();
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // =====================
        // == Helper ==
        // =====================
        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var v = value.Replace("\"", "\"\"");
            if (v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
                return "\"" + v + "\"";
            return v;
        }

        private IQueryable<Transaction> ScopeQuery(TransactionDTO q, int currentUserId, bool isAdmin)
        {
            var src = _db.Transactions.AsQueryable();
            if (!isAdmin)
                return src.Where(t => t.UserId == currentUserId);
            if (q.UserId > 0)
                return src.Where(t => t.UserId == q.UserId);
            return src;
        }

        private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> q, TransactionDTO f)
        {
            if (f.DateFrom.HasValue)
                q = q.Where(t => t.CreatedAt >= f.DateFrom.Value);
            if (f.DateTo.HasValue)
                q = q.Where(t => t.CreatedAt <= f.DateTo.Value);
            if (!string.IsNullOrWhiteSpace(f.FilterStatus))
                q = q.Where(t => t.Status == f.FilterStatus);
            if (!string.IsNullOrWhiteSpace(f.FilterType))
                q = q.Where(t => t.Purpose == f.FilterType);
            if (f.MinAmount.HasValue)
                q = q.Where(t => t.Amount >= f.MinAmount.Value);
            if (f.MaxAmount.HasValue)
                q = q.Where(t => t.Amount <= f.MaxAmount.Value);
            if (!string.IsNullOrWhiteSpace(f.Search))
                q = q.Where(t => t.ProviderTxnId!.Contains(f.Search));
            return q;
        }

        private static IQueryable<Transaction> ApplySort(IQueryable<Transaction> q, TransactionDTO s)
        {
            bool desc = s.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
            return s.SortBy?.ToLower() switch
            {
                "amount" => desc ? q.OrderByDescending(t => t.Amount).ThenByDescending(t => t.CreatedAt)
                                 : q.OrderBy(t => t.Amount).ThenByDescending(t => t.CreatedAt),
                "status" => desc ? q.OrderByDescending(t => t.Status).ThenByDescending(t => t.CreatedAt)
                                 : q.OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt),
                _ => desc ? q.OrderByDescending(t => t.CreatedAt)
                          : q.OrderBy(t => t.CreatedAt)
            };
        }

        private static Expression<Func<Transaction, TransactionDTO>> MapToDtoExpr()
        {
            return t => new TransactionDTO
            {
                TransactionId = t.TransactionId,
                UserId = t.UserId,
                Username = t.User.Username,
                Amount = t.Amount,
                Currency = t.Currency,
                PaymentMethod = t.PaymentMethod,
                ProviderTxnId = t.ProviderTxnId,
                Purpose = t.Purpose,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            };
        }

        private static TransactionDTO MapToDto(Transaction t)
        {
            return new TransactionDTO
            {
                TransactionId = t.TransactionId,
                UserId = t.UserId,
                Amount = t.Amount,
                Currency = t.Currency,
                PaymentMethod = t.PaymentMethod,
                ProviderTxnId = t.ProviderTxnId,
                Purpose = t.Purpose,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            };
        }
    }
}
