using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure.UserSecurity.Permissions
{ }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PermissionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var permissionFilter = context.HttpContext.RequestServices.GetRequiredService<PermissionAuthorizationFilter>();
        permissionFilter.OnAuthorization(context);
    }
}