using App.Application.Contracts.Repositories.IUserService;
using App.Domain.UserSecurity;
using AutoMapper;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;
using X.PagedList.Extensions;

namespace App.Infrastructure.Repositories.UserService
{ }
public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IMapper _mapper;

    public UserService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        SignInManager<User> signInManager,
        AppDbContext dbContext,
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        IPasswordHasher<User> passwordHasher,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<User?> FindByNameAsync(string username)
    {
        return await _userManager.FindByNameAsync(username);
    }

    public async Task<bool> CheckPasswordAsync(User user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<SignInResult> SignInAsync(User user, bool rememberMe, List<Claim> claims)
    {
        try
        {
            //var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return SignInResult.Failed;
            }
            // If password is correct, sign in with claims
            await _signInManager.SignInWithClaimsAsync(user, isPersistent: rememberMe, claims);
            return SignInResult.Success;
        }
        catch (Exception)
        {
            return SignInResult.Failed;
        }
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<User> CreateUser(UserDto user, string password, string roleName, int departmentId)
    {
        user.UserStatus = true;
        user.EmailConfirmed = true;

        User newUser = new User()
        {
            Id = user.Id,
            UserName = user.Username,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            Name = user.Name,
            UserStatus = user.UserStatus,
            FirstLogin = user.FirstLogin,
            IsActive = user.IsActive,
            IsDeleted = user.IsDeleted,
            CreatedBy = user.CreatedBy
        };

        await _userManager.CreateAsync(newUser, password);

        //get role by name
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        await _dbContext.UserRoles.AddAsync(new UserRole()
        {
            RoleId = role.Id,
            UserId = newUser.Id,
            AssignedDate = DateTime.UtcNow,
            DepartmentId = user.DepartmentId ?? departmentId,
        });
        await _dbContext.SaveChangesAsync();
        //await _userManager.AddToRoleAsync(newUser, roleName);

        return newUser;
    }

    public async Task<List<UserPasswordLog>> GetUserPasswordLogsAsync(string userId)
    {
        var result = await _dbContext.UserPasswordLog
        .Where(ucp => ucp.UserId == userId)
        .OrderByDescending(ucp => ucp.PasswordChange)
        .ToListAsync();

        return result;
    }

    public async Task<UserPasswordLog> GetLastUserPasswordLogsAsync(string userId)
    {
        var result = await _dbContext.UserPasswordLog
         .Where(ucp => ucp.UserId == userId)
         .OrderByDescending(ucp => ucp.PasswordChange)
         .AsNoTracking()
         .FirstOrDefaultAsync();

        return result;
    }

    public async Task<List<UserLoginLog>> GetUserLoginLogAsync(string userId)
    {
        var result = await _dbContext.UserLoginLog
        .Where(ucp => ucp.UserId == userId)
        .OrderByDescending(ucp => ucp.LogginDateTime)
        .ToListAsync();

        return result;
    }

    public async Task<bool> CreateUserLoginLogAsync(UserLoginLogDto userLoginLogDto)
    {
        var newRecord = await _dbContext.UserLoginLog
            .AddAsync(new UserLoginLog()
            {
                UserId = userLoginLogDto.UserId,
                IpAddress = userLoginLogDto.IpAddress,
                ActivityType = userLoginLogDto.ActivityType,
                Description = userLoginLogDto.Description,
                LogginDateTime = userLoginLogDto.LogginDateTime,
                LoggedOutDateTime = userLoginLogDto.LoggedOutDateTime,
            });
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IdentityResult> ChangePasswordAsync(User user, ResetPasswordDto model)
    {
        return await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        //_userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<IdentityResult> UpdateUserAsync(UserDto updatedUser)
    {
        var selectedUser = await _userManager.FindByIdAsync(updatedUser.Id);

        selectedUser.UserName = updatedUser.Username;
        selectedUser.Name = updatedUser.Name;
        selectedUser.Email = updatedUser.Email;
        selectedUser.IsActive = (bool)updatedUser.IsDeleted ? false : updatedUser.IsActive;
        selectedUser.IsDeleted = updatedUser.IsDeleted;
        selectedUser.LastModifiedBy = updatedUser.LastModifiedBy;
        selectedUser.LastModifiedDate = DateTime.Now;


        //get user roles
        var userRoles = await _dbContext.UserRoles
                        .Where(u => u.UserId == selectedUser.Id)
                        .ToListAsync();
        //delete exist user role
        if (userRoles.Count > 0)
        {
            foreach (var role in userRoles)
            {
                var roleToRemove = _dbContext.Roles.FirstOrDefault(r => r.Id == role.RoleId);
                await _userManager.RemoveFromRoleAsync(selectedUser, roleToRemove.Name);
            }
        }
        //if (userRoles?.Exists(r => r.RoleId == updatedUser.RoleId) == false)
        //{
        //var role = userRoles.Where(r => r.RoleId != updatedUser.RoleId).FirstOrDefault();
        var roleToAdd = _dbContext.Roles.FirstOrDefault(r => r.Id == updatedUser.RoleId);

        await _dbContext.UserRoles.AddAsync(new UserRole()
        {
            RoleId = roleToAdd.Id,
            UserId = selectedUser.Id,
            AssignedDate = DateTime.UtcNow,
            DepartmentId = (int)updatedUser.DepartmentId,
        });
        await _dbContext.SaveChangesAsync();
        //await _userManager.AddToRoleAsync(selectedUser, roleToAdd.Name);
        //}

        return await _userManager.UpdateAsync(selectedUser);
    }

    public async Task<IdentityResult> UpdateUserAsync(User updatedUser)
    {
        return await _userManager.UpdateAsync(updatedUser);
    }

    public async Task<bool> IsPasswordInRecentHistoryAsync(string userId, string oldPassword, string newPassword, int historyCount = 3)
    {
        //First get all password logs for this user
        var allPasswordLogs = await _unitOfWork.Repository<UserPasswordLog>()
                    .GetAsync(
        predicate: p => p.UserId == userId,
        includeString: null,
        disableTracking: true);

        //Then order them in memory
        var recentPasswords = allPasswordLogs
            .OrderByDescending(p => p.PasswordChange)
            .Take(3)
            .ToList();

        // Check if new password matches any of the recent passwords
        foreach (var passwordEntry in recentPasswords)
        {
            // If you're encrypting passwords (not recommended for passwords)
            string decryptedPassword = _encryptionService.Decrypt(passwordEntry.NewPassword);
            if (decryptedPassword == newPassword)
                return true;
        }
        return false;
    }

    public async Task<LoginResult> ValidateLoginAsync(string username, string password)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: Starting login validation for username: {username}");

            var user = await _userManager.FindByNameAsync(username);
            System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: User found: {user != null}");

            // User not found
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: User not found for username: {username}");
                return new LoginResult { Success = false, Message = "اسم المستخدم غير مسجل في النظام" };
            }

            // Check if account is already locked
            if ((bool)!user?.IsActive)
                return new LoginResult { Success = false, Message = "تم تعطيل الحساب، فضلا تواصل مع الدعم الفني" };

            // Check if account was deleted
            if (user.IsDeleted == null)
            {
                user.IsDeleted = false;
                await _userManager.UpdateAsync(user);
            }
            if ((bool)user?.IsDeleted)
                return new LoginResult { Success = false, Message = "تم حذف الحساب، فضلا تواصل مع الدعم الفني" };

            // Check if PasswordHash is null or empty
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: PasswordHash is null or empty for user: {username}");
                return new LoginResult { Success = false, Message = "خطأ في بيانات المستخدم، فضلا تواصل مع الدعم الفني" };
            }

            System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: PasswordHash exists, validating password for user: {username}");

            // Validate password
            var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: Password validation result: {passwordResult}");

            if (passwordResult == PasswordVerificationResult.Success || passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // Password correct - reset failed attempts
                if (user.AccessFailedCount > 0)
                {
                    user.AccessFailedCount = 0;
                    await _userManager.UpdateAsync(user);
                }

                // If rehash is needed, update the password hash with the new algorithm
                if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: Password rehash needed for user: {username}");
                    var newPasswordHash = _passwordHasher.HashPassword(user, password);
                    user.PasswordHash = newPasswordHash;
                    await _userManager.UpdateAsync(user);
                    System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync: Password hash updated for user: {username}");
                }

                return new LoginResult { Success = true, UserId = user.Id };
            }
            // Increment failed attempts
            user.AccessFailedCount++;

            // Check if we need to lock the account
            if (user.AccessFailedCount >= 3)
            {
                user.IsActive = false;
                user.LockoutEnd = DateTime.UtcNow.AddDays(3); // Optional: Auto-unlock after 24 hours

                // Log the account lockout
                var lockoutLog = new UserLoginLog
                {
                    UserId = user.Id,
                    ActivityType = "AccountLockout",
                    Description = "Account locked due to multiple failed login attempts",
                    IpAddress = "System", // You might want to pass the IP address
                    LogginDateTime = DateTime.UtcNow,
                    CreatedById = user.Id,
                };

                await _unitOfWork.Repository<UserLoginLog>().AddAsync(lockoutLog);
                await _unitOfWork.Complete();
            }

            // Save changes into User table
            await _userManager.UpdateAsync(user);

            string message = user.AccessFailedCount > 3
                ? "تم تعطيل الحساب لإدخال كلمة مرور خاطئة اكثر من 3 مرات، فضلا تواصل مع الدعم الفني"
                : $"كلمة مرور خاطئة متبقي فقط {3 - user.AccessFailedCount} محاولات وسيتم تعطيل الحساب";

            return new LoginResult
            {
                Success = false,
                Message = message,
                RemainingAttempts = Math.Max(0, 3 - user.AccessFailedCount)
            };
        }
        catch (Exception ex)
        {
            // Log the exception here if you have logging
            System.Diagnostics.Debug.WriteLine($"ValidateLoginAsync Error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

            return new LoginResult
            {
                Success = false,
                Message = "حدث خطأ في النظام، نرجو المحاولة مرة أخرى"
            };
        }
    }

    /// <summary>
    /// Method to unlock an account (for Admin use)
    /// <param name="userId"></param>
    /// </summary>        
    public async Task<bool> UnlockAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        user.IsActive = true;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        await _userManager.UpdateAsync(user);

        return true;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
    {
        var userRoles = await _dbContext.UserRoles.AsNoTracking()
                        .Where(ur => ur.UserId == userId)
                        .Join(_dbContext.Roles,
                            userRole => userRole.RoleId,
                            role => role.Id,
                            (userRole, role) => role.Name)
                        .ToListAsync();

        return userRoles;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsers()
    {
        var users = _mapper.Map<List<UserDto>>(await _userManager.Users.AsNoTracking().ToListAsync());
        return (IReadOnlyList<UserDto>)users;
    }

    public async Task<PaginatedResult<UserDto>> GetPaginatedUsers(
        int pageNumber = 1,
        int pageSize = 10,
        string searchString = "",
        int sortColumn = 0,
        string sortDirection = "asc",
        bool includeSuperAdminUsers = true)
    {
        // Start with all users and include related data for searching
        IQueryable<User> usersQuery = _userManager.Users.AsNoTracking();

        // Get total count before filtering for search
        var totalCount = await usersQuery.CountAsync();

        // Get all users first (since we need to search in related entities)
        var allUsers = await usersQuery.ToListAsync();

        // Manually map to DTOs with related data
        var userDtos = new List<UserDto>();
        foreach (var user in allUsers)
        {
            var userDto = _mapper.Map<UserDto>(user);

            // Get user role and department information
            var userRoles = await _userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault();

            if (!string.IsNullOrEmpty(roleName))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    userDto.RoleName = role.Name;
                    userDto.RoleNameArabic = role.RoleNameArabic;
                }
            }

            // Get department information from UserRole
            var userRole = await _dbContext.UserRoles
                .Include(ur => ur.Department)
                    .ThenInclude(d => d.DepartmentType)
                .FirstOrDefaultAsync(ur => ur.UserId == user.Id);

            if (userRole?.Department != null)
            {
                userDto.Department = userRole.Department.Name;
                userDto.DepartmentType = userRole.Department.DepartmentType?.DepartmentTypeName;
            }

            userDtos.Add(userDto);
        }

        // Apply search filtering on DTOs (after we have all related data)
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.ToLower();
            userDtos = userDtos.Where(u =>
                (u.Username?.ToLower().Contains(searchString) ?? false) ||
                (u.Email?.ToLower().Contains(searchString) ?? false) ||
                (u.PhoneNumber?.Contains(searchString) ?? false) ||
                (u.Name?.ToLower().Contains(searchString) ?? false) ||
                (u.Department?.ToLower().Contains(searchString) ?? false) ||
                (u.DepartmentType?.ToLower().Contains(searchString) ?? false) ||
                (u.RoleName?.ToLower().Contains(searchString) ?? false) ||
                (u.RoleNameArabic?.ToLower().Contains(searchString) ?? false)
            ).ToList();
        }

        // Apply Super Admin filtering based on user role
        if (!includeSuperAdminUsers)
        {
            // Filter out users with Super Admin role
            userDtos = userDtos.Where(u =>
                u.RoleName != "Super Admin" &&
                u.RoleNameArabic != "مدير النظام"
            ).ToList();
        }

        // Update total count after filtering
        totalCount = userDtos.Count;

        // Apply sorting
        userDtos = ApplySortingToDtos(userDtos, sortColumn, sortDirection);
        return new PaginatedResult<UserDto>
        {
            Items = userDtos.ToPagedList<UserDto>(pageNumber, pageSize),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchString = searchString
        };
    }

    private List<UserDto> ApplySortingToDtos(List<UserDto> userDtos, int sortColumn, string sortDirection)
    {
        switch (sortColumn)
        {
            case 0: // Name
                return sortDirection.ToLower() == "desc"
                    ? userDtos.OrderByDescending(u => u.Name).ToList()
                    : userDtos.OrderBy(u => u.Name).ToList();
            case 1: // Username
                return sortDirection.ToLower() == "desc"
                    ? userDtos.OrderByDescending(u => u.Username).ToList()
                    : userDtos.OrderBy(u => u.Username).ToList();
            case 2: // Department
                return sortDirection.ToLower() == "desc"
                    ? userDtos.OrderByDescending(u => u.Department).ToList()
                    : userDtos.OrderBy(u => u.Department).ToList();
            case 3: // Department Type
                return sortDirection.ToLower() == "desc"
                    ? userDtos.OrderByDescending(u => u.DepartmentType).ToList()
                    : userDtos.OrderBy(u => u.DepartmentType).ToList();
            case 4: // Role
                return sortDirection.ToLower() == "desc"
                    ? userDtos.OrderByDescending(u => u.RoleNameArabic ?? u.RoleName).ToList()
                    : userDtos.OrderBy(u => u.RoleNameArabic ?? u.RoleName).ToList();
            default:
                return userDtos.OrderBy(u => u.Name).ToList();
        }
    }

    private IQueryable<User> ApplySorting(
        IQueryable<User> query,
        int sortColumn,
        string sortDirection)
    {
        // Map column index to property name
        var columnMap = new Dictionary<int, Expression<Func<User, object>>>
        {
            { 0, u => u.Id },
            { 1, u => u.UserName },
            { 2, u => u.Email },
            { 3, u => u.PhoneNumber },
            { 4, u => u.Name }
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
            query = query.OrderBy(u => u.UserName);
        }

        return query;
    }

    public async Task<string> GeneratePasswordResetTokenAsync(User user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<List<UserRole>> GetUserRoles(string userId)
    {
        var userRoles = await _dbContext.UserRoles
            .Include(ur => ur.Department)
            .Include(ur => ur.Department.DepartmentType)
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        return userRoles;
    }
}

