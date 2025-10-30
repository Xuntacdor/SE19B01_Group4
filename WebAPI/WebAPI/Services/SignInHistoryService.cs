using System;
using System.Collections.Generic;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class SignInHistoryService : ISignInHistoryService
    {
        private readonly IUserSignInHistoryRepository _repository;

        public SignInHistoryService(IUserSignInHistoryRepository repository)
        {
            _repository = repository;
        }

        public void LogSignIn(int userId, string? ipAddress, string? deviceInfo)
        {
            var history = new UserSignInHistory
            {
                UserId = userId,
                IpAddress = ipAddress,
                DeviceInfo = deviceInfo,
                SignedInAt = DateTime.UtcNow
            };

            _repository.Add(history);
            _repository.SaveChanges();
        }

        public IEnumerable<UserSignInHistory> GetUserHistory(int userId, int limit = 30)
        {
            return _repository.GetUserHistory(userId, limit);
        }
    }
}
