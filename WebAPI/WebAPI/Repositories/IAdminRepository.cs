using System.Collections.Generic;

namespace WebAPI.Repositories
{
    public interface IAdminRepository
    {
        int CountUsers();
        int CountExams();
        decimal GetTotalPaidTransactions();
        int CountExamAttempts();
        IEnumerable<object> GetMonthlySalesTrend();
    }
}
