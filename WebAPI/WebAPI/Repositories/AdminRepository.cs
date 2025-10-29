using System.Collections.Generic;
using System.Linq;
using WebAPI.Data;

namespace WebAPI.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _db;
        public AdminRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public int CountUsers() => _db.User.Count();
        public int CountExams() => _db.Exam.Count();

        public decimal GetTotalPaidTransactions()
        {
            return _db.Transactions
                .Where(t => t.Status == "PAID")
                .Sum(t => (decimal?)t.Amount) ?? 0;
        }

        public int CountExamAttempts() => _db.ExamAttempt.Count();

        public IEnumerable<object> GetMonthlySalesTrend()
        {
            return _db.Transactions
                .Where(t => t.Status == "PAID")
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new
                {
                    year = g.Key.Year,
                    month = g.Key.Month,
                    total = g.Sum(x => x.Amount)
                })
                .OrderBy(g => g.year)
                .ThenBy(g => g.month)
                .ToList();
        }
    }
}
