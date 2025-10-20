using App.Application.Contracts.Repositories.IUserService;
using App.Domain.UserSecurity;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using X.PagedList.Extensions;

namespace App.Infrastructure.Repositories.UserService
{ }
public class RoleService : IRoleService
{
    private readonly RoleManager<Role> _roleManager;
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;

    public RoleService(RoleManager<Role> roleManager, AppDbContext dbContext, IMapper mapper)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Role?> FindByNameAsync(string roleName)
    {
        return await _roleManager.FindByNameAsync(roleName);
    }

    public async Task<IdentityResult> CreateRoleAsync(RoleDto role)
    {
        if (await _roleManager.RoleExistsAsync(role.Name))
        {
            return IdentityResult.Failed(new IdentityError { Description = "Role already exists." });
        }

        var newRole = new Role()
        {
            Id = role.Id,
            Name = role.Name,
            RoleNameArabic = role.RoleNameArabic,
            Description = role.Description,
            IsDeleted = role.IsDeleted,
            CreatedById = role.CreatedById,
            CreatedDate = DateTime.Now
        };
        return await _roleManager.CreateAsync(newRole);
    }

    public async Task<IdentityResult> DeleteRoleAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            return IdentityResult.Failed(new IdentityError { Description = "Role not found." });

        return await _roleManager.DeleteAsync(role);
    }

    public async Task<IReadOnlyList<Role>> GetAllRolesAsync()
    {
        var roles = await Task.FromResult(_roleManager.Roles);
        return (IReadOnlyList<Role>)roles.ToList();
    }

    public async Task AddClaimsToRole(User user, Role role, List<Claim> claims)
    {
        var roleClaims = new List<RoleClaim>();
        foreach (var clm in claims)
        {
            var roleClaim = new RoleClaim
            {
                RoleId = role.Id,
                ClaimType = clm.Value,
                ClaimValue = clm.Value,
                CreatedById = user.Id
            };
            roleClaims.Add(roleClaim);
        }
        // Add list of claims to role
        if (roleClaims.Count > 0)
        {
            await _dbContext.RoleClaims.AddRangeAsync(roleClaims);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddClaimsToRole(string userId, RoleClaimsDto roleClaim)
    {
        //Get role
        var role = await FindByIdAsync(roleClaim.RoleId);
        //Get all the role claims that exists
        var claims = await _dbContext.RoleClaims.Where(rc => rc.RoleId == roleClaim.RoleId).ToListAsync();
        var roleClaims = new List<RoleClaim>();

        //remove unselected claims first
        var unselectClaims = roleClaim.Cliams.Where(rc => rc.IsSelected == false).ToList();

        foreach (var clm in roleClaim.Cliams)
        {
            //check claim not selected
            if (claims.Count > 0)
            {
                //check claim exist in role claim - if exist remove it
                if (!clm.IsSelected)
                {
                    if (claims.Exists(rc => rc.ClaimValue == clm.ClaimValue))
                    {
                        var claimToRemove = claims.FirstOrDefault(rc => rc.ClaimValue == clm.ClaimValue);
                        _dbContext.Remove(claimToRemove);
                        _dbContext.SaveChangesAsync();
                    }
                }
            }

            if (clm.IsSelected && !claims.Exists(rc => rc.ClaimValue == clm.ClaimValue))
            {
                var roleClaimToAdd = new RoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = clm.ClaimValue,
                    ClaimValue = clm.ClaimValue,
                    Description = clm.Description,
                    CreatedById = userId
                };
                roleClaims.Add(roleClaimToAdd);
            }

        }
        // Add list of claims to role
        if (roleClaims.Count > 0)
        {
            await _dbContext.RoleClaims.AddRangeAsync(roleClaims);
            await _dbContext.SaveChangesAsync(userId);
        }
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }

    public async Task<IEnumerable<Claim>> GetRoleClaimsAsync(string roleName)
    {
        // First get the role ID
        var role = await FindByNameAsync(roleName);

        var roleId = role?.Id;

        if (roleId == null)
            return new List<Claim>();

        // Then get claims for this role
        var roleClaims = await _dbContext.RoleClaims.AsNoTracking().Where(r => r.RoleId == roleId).ToListAsync();

        return roleClaims
            .Select(rc => new Claim(rc.ClaimType, rc.ClaimValue))
            .ToList();
    }

    public async Task<IEnumerable<Claim>> GetRoleClaimsByIdAsync(string Id)
    {
        // First get the role ID
        var role = await FindByIdAsync(Id);

        var roleId = role?.Id;

        if (roleId == null)
            return new List<Claim>();

        // Then get claims for this role
        var roleClaims = await _dbContext.RoleClaims.AsNoTracking().Where(r => r.RoleId == roleId).ToListAsync();

        return roleClaims
            .Select(rc => new Claim(rc.ClaimType, rc.ClaimValue, rc.ClaimNameArabic))
            .ToList();
    }

    public async Task<RolePermission> GetClaimsByValueAsync(string value)
    {
        var claim = await _dbContext.RoleClaims.AsNoTracking().Where(r => r.ClaimValue == value).FirstOrDefaultAsync();
        if (claim == null)
            return new RolePermission();
        RolePermission rolePermission = new()
        {
            ClaimType = claim.ClaimType,
            ClaimValue = claim.ClaimValue,
            ClaimNameArabic = claim.ClaimNameArabic,
        };
        return rolePermission;
    }

    public async Task<IEnumerable<Claim>> GetAllUserClaimsAsync(string userId)
    {
        var claims = new List<Claim>();

        // Get user roles
        var roles = _dbContext.UserRoles.AsNoTracking()
                        .Where(ur => ur.UserId == userId)
                        .Join(_dbContext.Roles,
                            userRole => userRole.RoleId,
                            role => role.Id,
                            (userRole, role) => role.Name)
                        .ToList();

        // Get claims from each role
        foreach (var role in roles)
        {
            var roleClaims = await GetRoleClaimsAsync(role);
            claims.AddRange(roleClaims);
        }

        return claims;
    }

    public async Task<PaginatedResult<RoleDto>> GetPaginatedRoles(
    int pageNumber = 1,
    int pageSize = 10,
    string searchString = "",
    int sortColumn = 0,
    string sortDirection = "asc")
    {
        // Start with all users
        IQueryable<Role> roleQuery = _roleManager.Roles.AsNoTracking();

        // Apply filtering if search term is provided
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.ToLower();
            roleQuery = roleQuery.Where(u =>
                u.Name.ToLower().Contains(searchString) ||
                u.RoleNameArabic.ToLower().Contains(searchString) ||
                u.Description.Contains(searchString)
            );
        }

        // Get total count before pagination
        var totalCount = await roleQuery.CountAsync();

        // Apply sorting
        roleQuery = ApplySorting(roleQuery, sortColumn, sortDirection);

        // Map to DTOs
        var roleDtos = _mapper.Map<List<RoleDto>>(roleQuery);
        return new PaginatedResult<RoleDto>
        {
            Items = roleDtos.ToPagedList<RoleDto>(pageNumber, pageSize),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchString = searchString
        };
    }

    private IQueryable<Role> ApplySorting(
        IQueryable<Role> query,
        int sortColumn,
        string sortDirection)
    {
        // Map column index to property name
        var columnMap = new Dictionary<int, Expression<Func<Role, object>>>
    {
        { 0, u => u.Id },
        { 1, u => u.Name },
        { 2, u => u.RoleNameArabic },
        { 3, u => u.Description },
        { 4, u => u.CreatedDate }
        // Add more mappings according to your column structure
    };

        // Get the property expression based on column index
        if (columnMap.TryGetValue(sortColumn, out var sortProperty))
        {
            // Apply sorting
            if (sortDirection.ToLower() == "asc")
            {
                query = query.OrderBy(sortProperty);
            }
            else
            {
                query = query.OrderByDescending(sortProperty);
            }
        }
        else
        {
            // Default sorting if column not found
            query = query.OrderByDescending(u => u.CreatedDate);
        }

        return query;
    }

    public async Task<Role> FindByIdAsync(string Id)
    {
        return await _roleManager.FindByIdAsync(Id);
    }

    public async Task<IdentityResult> UpdateRoleAsync(RoleDto role)
    {
        var existRole = await _roleManager.FindByNameAsync(role.Name);
        if (existRole == null)
            return IdentityResult.Failed(new IdentityError { Description = "Role already exists." });

        existRole.Name = role.Name;
        existRole.RoleNameArabic = role.RoleNameArabic;
        existRole.Description = role.Description;
        existRole.IsDeleted = role.IsDeleted;
        existRole.LastModifiedById = role.CreatedById;
        existRole.LastModifiedDate = DateTime.Now;

        return await _roleManager.UpdateAsync(existRole);
    }

    public async Task<RoleClaimsDto> GetClaimsAddPermissionseAsync(string roleId)
    {
        var role = await FindByIdAsync(roleId);
        //get all exist role claims (permissions)
        var existRoleClaims = await GetRoleClaimsByIdAsync(roleId);

        var model = new RoleClaimsDto
        {
            RoleId = roleId,
            RoleName = role.Name,
            RoleNameArabic = role.RoleNameArabic
        };

        // Loop through each claim we have in our application
        foreach (Claim claim in GetAllClaimsPermissions.GetAllControllerActionsUpdated())
        {
            var roleClaim = new RolePermission
            {
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            };
            //get claim arabic value if changed in db
            var claimInDb = await GetClaimsByValueAsync(claim.Value);
            roleClaim.ClaimNameArabic = claimInDb.ClaimNameArabic;

            // If the role has the claim, set IsSelected property to true, so the checkbox
            // next to the claim is checked on the UI
            if (existRoleClaims.Any(c => c.Value == claim.Value))
                roleClaim.IsSelected = true;

            model.Cliams.Add(roleClaim);
        }

        return model;
    }
}

