using Microsoft.AspNetCore.Mvc;
using Universalis.Mogboard.Identity;

namespace Universalis.Application.Controllers;

public class RequireMogboardAuthorizationAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Requires that the request user is authorized to perform the current action.
    /// </summary>
    /// <param name="roles">
    /// The required roles. If multiple roles are joined together, either of them may access the resource.
    /// If this attribute is applied multiple times, all of the provided roles must be fulfilled to access
    /// the requested resource.
    /// </param>
    public RequireMogboardAuthorizationAttribute(Roles roles = Roles.User) : base(typeof(RequireMogboardAuthorizationFilter))
    {
        Arguments = new object[] { roles };
    }
}