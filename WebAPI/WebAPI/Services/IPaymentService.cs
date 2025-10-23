namespace WebAPI.Services
{
    public interface IPaymentService
    {
        string CreateVipCheckoutSession(int planId, int userId);
    }
}
