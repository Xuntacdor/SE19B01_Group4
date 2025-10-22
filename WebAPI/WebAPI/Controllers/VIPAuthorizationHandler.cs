using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WebAPI.Services.Authorization;

namespace WebAPI.Authorization
{
    public class VIPRequirement : IAuthorizationRequirement { }

    public class VIPAuthorizationHandler : AuthorizationHandler<VIPRequirement>
    {
        private readonly IVipAuthorizationService _vipAuthService;

        public VIPAuthorizationHandler(IVipAuthorizationService vipAuthService)
        {
            _vipAuthService = vipAuthService;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, VIPRequirement requirement)
        {
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
                return Task.CompletedTask;

            if (!int.TryParse(claim.Value, out var userId))
                return Task.CompletedTask;

            if (_vipAuthService.IsUserVip(userId))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
