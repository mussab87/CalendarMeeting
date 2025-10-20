using App.Domain.UserSecurity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace App.Application.Contracts.Repositories.IUserService
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(string userId);
        Task<User?> FindByNameAsync(string username);
        Task<bool> CheckPasswordAsync(User user, string password);
        Task<SignInResult> SignInAsync(User user, bool rememberMe, List<Claim> claims);
        Task SignOutAsync();
        Task<User> CreateUser(UserDto user, string password, string roleName, int departmentId);

        Task<List<UserPasswordLog>> GetUserPasswordLogsAsync(string userId);
        Task<UserPasswordLog> GetLastUserPasswordLogsAsync(string userId);

        Task<List<UserLoginLog>> GetUserLoginLogAsync(string userId);
        Task<bool> CreateUserLoginLogAsync(UserLoginLogDto userLoginLogDto);

        Task<IdentityResult> ChangePasswordAsync(User user, ResetPasswordDto resetPasswordDto);

        Task<IdentityResult> UpdateUserAsync(UserDto updatedUser);
        Task<IdentityResult> UpdateUserAsync(User updatedUser);

        Task<bool> IsPasswordInRecentHistoryAsync(string userId, string oldPassword, string newPassword, int historyCount = 3);

        Task<LoginResult> ValidateLoginAsync(string username, string password);
        Task<bool> UnlockAccountAsync(string userId);

        Task<IEnumerable<string>> GetUserRolesAsync(string userId);

        Task<List<UserRole>> GetUserRoles(string userId);
        Task<IReadOnlyList<UserDto>> GetAllUsers();

        Task<string> GeneratePasswordResetTokenAsync(User user);
        Task<PaginatedResult<UserDto>> GetPaginatedUsers(
            int pageNumber = 1,
            int pageSize = 10,
            string searchString = "",
            int sortColumn = 0,
            string sortDirection = "asc",
            bool includeSuperAdminUsers = true);
    }
}

