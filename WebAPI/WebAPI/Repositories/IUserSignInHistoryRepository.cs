using WebAPI.Models;
using System.Collections.Generic;

namespace WebAPI.Repositories
{
    public interface IUserSignInHistoryRepository
    {
        void Add(UserSignInHistory history);
        IEnumerable<UserSignInHistory> GetUserHistory(int userId, int limit = 30);
        void SaveChanges();
    }
}