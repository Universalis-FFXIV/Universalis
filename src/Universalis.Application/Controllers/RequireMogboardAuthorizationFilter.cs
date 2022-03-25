using System;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Universalis.Mogboard.Identity;

namespace Universalis.Application.Controllers;

public class RequireMogboardAuthorizationFilter : IAsyncActionFilter
{
    private readonly IMogboardAuthenticationService _auth;
    private readonly ILogger<RequireMogboardAuthorizationFilter> _logger;

    private readonly Roles _roles;

    public RequireMogboardAuthorizationFilter(Roles roles, IMogboardAuthenticationService auth, ILogger<RequireMogboardAuthorizationFilter> logger)
    {
        _auth = auth;
        _logger = logger;

        _roles = roles;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get the session cookie
        var cookie = context.HttpContext.Request.Cookies["session"];
        if (cookie == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Retrieve the authenticated user
        MogboardUser user;
        try
        {
            user = await _auth.Authenticate(cookie);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception thrown while attempting to authenticate user.");
            context.Result = new ForbidResult();
            return;
        }

        // Check the user's authorization state
        var authorized =
            _roles.HasFlag(Roles.User) /* && true */ ||
            _roles.HasFlag(Roles.Admin) && user.IsAdmin();
        if (!authorized)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Set the authorized user on the action arguments
        context.HttpContext.Items["user"] = user;
        await next();
    }
}