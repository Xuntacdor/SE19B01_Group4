using System.Collections.Generic;

namespace WebAPI.Services
{
    public interface IAdminService
    {
        (int totalUsers, int totalExams, decimal totalTransactions, int totalAttempts) GetDashboardStats();
        IEnumerable<object> GetSalesTrend();
    }
}
