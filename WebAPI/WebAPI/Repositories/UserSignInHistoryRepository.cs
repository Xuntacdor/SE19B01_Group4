using WebAPI.Data;
using WebAPI.Models;
using System.Collections.Generic;
using System.Linq;

namespace WebAPI.Repositories
{
    public class UserSignInHistoryRepository : IUserSignInHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public UserSignInHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public void Add(UserSignInHistory history)
        {
            _context.UserSignInHistory.Add(history);
        }

        public IEnumerable<UserSignInHistory> GetUserHistory(int userId, int limit = 30)
        {
            return _context.UserSignInHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.SignedInAt)
                .Take(limit)
                .ToList();
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}