using WebAPI.DTOs;

namespace WebAPI.Services.Authorization
{
    public interface IVipAuthorizationService
    {
        bool IsUserVip(int userId);
        DateTime GetVipExpireDate(int userId);
        void EnsureVipAccess(int userId, string feature);
        VipStatusDTO GetVipStatus(int userId);
    }
}
