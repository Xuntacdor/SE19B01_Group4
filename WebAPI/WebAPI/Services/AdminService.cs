using System.Collections.Generic;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _repo;

        public AdminService(IAdminRepository repo)
        {
            _repo = repo;
        }

        public (int totalUsers, int totalExams, decimal totalTransactions, int totalAttempts) GetDashboardStats()
        {
            return (
                _repo.CountUsers(),
                _repo.CountExams(),
                _repo.GetTotalPaidTransactions(),
                _repo.CountExamAttempts()
            );
        }

        public IEnumerable<object> GetSalesTrend()
        {
            return _repo.GetMonthlySalesTrend();
        }
    }
}
