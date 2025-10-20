using App.Domain.UserSecurity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace App.Infrastructure.UserSecurity;

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
{
    private readonly AppDbContext _context;

    public CustomUserClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IOptions<IdentityOptions> options,
        AppDbContext context)
        : base(userManager, roleManager, options)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add custom claims - Get user role with department directly from DbContext
        var userRole = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .FirstOrDefaultAsync();

        // Add department ID claim
        if (userRole != null)
        {
            identity.AddClaim(new Claim("deptId", userRole.DepartmentId.ToString()));
        }

        // Add user name claim (display name)
        if (!string.IsNullOrEmpty(user.Name))
        {
            identity.AddClaim(new Claim("name", user.Name));
        }

        System.Diagnostics.Debug.WriteLine($"=== CustomUserClaimsPrincipalFactory ===");
        System.Diagnostics.Debug.WriteLine($"User: {user.UserName}");
        System.Diagnostics.Debug.WriteLine($"Department ID: {userRole?.DepartmentId.ToString() ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"Claims added: {identity.Claims.Count()}");

        return identity;
    }
}

