using Microsoft.Extensions.Caching.Memory;

namespace App.Infrastructure.UserSecurity.Permissions
{ }
public class PermissionService
{
    private readonly IMemoryCache _cache;

    public PermissionService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Call this method whenever permissions are updated
    /// </summary>
    /// <param name="userId"></param>
    public void InvalidateUserPermissionsCache(string userId)
    {
        string cacheKey = $"UserPermissions_{userId}";
        _cache.Remove(cacheKey);
    }
}

