using App.Domain.UserSecurity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;

namespace App.Application.Contracts.Repositories.IUserService
{
    public interface IRoleService
    {
        Task<Role?> FindByNameAsync(string roleName);
        Task<Role?> FindByIdAsync(string Id);
        Task<IdentityResult> CreateRoleAsync(RoleDto role);
        Task<IdentityResult> UpdateRoleAsync(RoleDto role);
        Task<IdentityResult> DeleteRoleAsync(string roleName);
        Task<IReadOnlyList<Role>> GetAllRolesAsync();
        Task<bool> RoleExistsAsync(string roleName);
        Task AddClaimsToRole(User user, Role role, List<Claim> claims);

        Task AddClaimsToRole(string userId, RoleClaimsDto roleClaim);

        Task<IEnumerable<Claim>> GetRoleClaimsAsync(string roleName);

        Task<IEnumerable<Claim>> GetRoleClaimsByIdAsync(string Id);

        Task<RolePermission> GetClaimsByValueAsync(string value);

        Task<RoleClaimsDto> GetClaimsAddPermissionseAsync(string roleId);

        Task<IEnumerable<Claim>> GetAllUserClaimsAsync(string userId);

        Task<PaginatedResult<RoleDto>> GetPaginatedRoles(
            int pageNumber = 1,
            int pageSize = 10,
            string searchString = "",
            int sortColumn = 0,
            string sortDirection = "asc");
    }
}

