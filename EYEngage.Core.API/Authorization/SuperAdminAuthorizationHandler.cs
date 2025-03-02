using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using EYEngage.Core.Domain;

namespace EYEngage.Core.API.Authorization;

public class SuperAdminAuthorizationHandler : AuthorizationHandler<SuperAdminRequirement>
{
    private readonly UserManager<User> _userManager;

    public SuperAdminAuthorizationHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        var user = await _userManager.GetUserAsync(context.User);

        if (user != null && await _userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            context.Succeed(requirement);
        }
    }
}

public class SuperAdminRequirement : IAuthorizationRequirement { }