using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;



namespace EYEngage.Core.API.Authorization;



public class ActiveUserRequirement : IAuthorizationRequirement { }

public class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly UserManager<User> _userManager;

    public ActiveUserAuthorizationHandler(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveUserRequirement requirement)
    {
        var user = await _userManager.GetUserAsync(context.User);

        if (user != null && (user.IsActive || await _userManager.IsInRoleAsync(user, "SuperAdmin")))
        {
            context.Succeed(requirement);
        }
    }
}