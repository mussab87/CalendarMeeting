using App.Application.Contracts.Repositories.IUserService;
using App.Helper.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Linq;
using System.Security.Claims;

namespace App.Infrastructure.UserSecurity.Permissions
{ }

public class PermissionAuthorizationFilter : IAuthorizationFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    //private readonly AppDbContext _dbContext;
    //private readonly ICacheService _cache;
    //private readonly IUserService _userService;
    //private readonly IRoleService _roleService;
    private readonly IServiceScopeFactory _scopeFactory;

    public PermissionAuthorizationFilter(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
    }

    public async Task OnAuthorization(AuthorizationFilterContext context)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var _userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var _roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
            var _cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var user = _httpContextAccessor.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Account" }, { "action", "Login" } });
                return;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Error" }, { "action", "Index" } });
                return;
            }

            // Cache key for permissions
            string cacheKey = $"UserPermissions_{userId}";
            var cachedData = await _cache.GetAsync<List<string>>(cacheKey);

            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();
            string requiredPermission = $"{controllerName} - {actionName}";


            var allUserPermissions = await _roleService.GetAllUserClaimsAsync(userId);
            List<string> rolePermissions = null;

            if (cachedData == null)
            {
                //var userRoles = user.Claims
                //                .Where(c => c.Type == ClaimTypes.Role)
                //                .Select(c => c.Value)
                //                .ToList(); //await _dbContext.UserRoles.Where(r => r.UserId == userId).ToListAsync();

                if (allUserPermissions.Any())
                {
                    if (!allUserPermissions.ToList().Exists(permission => permission.Value == requiredPermission))
                    {
                        context.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Error" }, { "action", "Index" } });
                        return;
                    }
                    rolePermissions = allUserPermissions.ToList()
                                       .Select(rc => rc.Value)
                                       .ToList();

                    // Save to cache
                    await _cache.SetAsync<List<string>>(cacheKey, rolePermissions, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
                }
                else
                {
                    context.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Error" }, { "action", "Index" } });
                    return;
                }
            }
            else
            {
                // Cache hit - deserialize the cached data            
                rolePermissions = cachedData;

                if (!rolePermissions.Contains(requiredPermission))
                {
                    //check in case Admin Role and permission not assigned to him add that role permission auto
                    var userRole = user.IsInRole(Roles.SuperAdmin) ? Roles.SuperAdmin : user.IsInRole(Roles.Admin) ? Roles.Admin : null;
                    if (userRole is not null)
                    {
                        //check if exist in db or not 
                        if (!allUserPermissions.ToList().Exists(rc => rc.Value == requiredPermission))
                            await AddPermissionToAdminSuperAdmin(userId, requiredPermission, userRole, _userService, _roleService);


                        allUserPermissions = await _roleService.GetAllUserClaimsAsync(userId);
                        rolePermissions = allUserPermissions.ToList()
                                       .Select(rc => rc.Value)
                                       .ToList();

                        // Remove and Save again to cache
                        await _cache.RemoveAsync(cacheKey);
                        await _cache.SetAsync<List<string>>(cacheKey, rolePermissions, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
                    }

                    if (!allUserPermissions.ToList().Exists(permission => permission.Value == requiredPermission))
                    {
                        context.Result = new RedirectToRouteResult(new RouteValueDictionary { { "controller", "Error" }, { "action", "Index" } });
                        return;
                    }
                }
            }
        }
    }

    private async Task AddPermissionToAdminSuperAdmin(string userId, string requiredPermission, string userRole, IUserService userService, IRoleService roleService)
    {
        try
        {
            var OneClaims = new List<Claim>();
            var claim = new Claim(requiredPermission, requiredPermission);
            OneClaims.Add(claim);
            var adminUser = await userService.GetUserByIdAsync(userId);
            var role = await roleService.FindByNameAsync(userRole);

            await roleService.AddClaimsToRole(adminUser, role, OneClaims);
        }
        catch (Exception ex)
        {
        }
    }

    void IAuthorizationFilter.OnAuthorization(AuthorizationFilterContext context)
    {
        throw new NotImplementedException();
    }
}


