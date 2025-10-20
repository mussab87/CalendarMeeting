using App.Helper.Dto;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Helper.Constants;
using App.Domain.UserSecurity;

namespace App.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(IServiceProvider serviceProvider) : base(serviceProvider)
    { }

    #region Login

    [AllowAnonymous]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get user 
            var user = await _userService.FindByNameAsync(model.Username);
            // Check account exist
            if (user == null)
            {
                ModelState.AddModelError("Username", "هذا المستخدم غير مسجل في النظام !");
                return View(model);
            }

            // Check if password entered wrong more than 3 times account must be deActivated
            // Check account IsActive
            var resultPasswordChecking = await _userService.ValidateLoginAsync(model.Username, model.Password);
            if (!resultPasswordChecking.Success)
            {
                ModelState.AddModelError("Password", $"{resultPasswordChecking.Message}");
                return View(model);
            }

            // Retrieve CAPTCHA from session
            var storedCaptcha = HttpContext.Session.GetString("CaptchaCode");
            // Check CAPTCHA Entered Correct Here
            if (string.IsNullOrEmpty(storedCaptcha) || model.CaptchaInput != storedCaptcha)
            {
                ModelState.AddModelError("CaptchaInput", "رمز التحقق غير صحيح!");
                return View(model);
            }

            // Check user password expire - in case password not changed more than 3 months - 90 days
            var lastPasswordChange = await _userService.GetLastUserPasswordLogsAsync(user.Id);
            int daysRemaining = 0;
            DateTime lastPasswordChangeDate = lastPasswordChange != null
                                        ? lastPasswordChange.PasswordChange
                                        : DateTime.UtcNow.AddDays(-80);

            int daysSinceLastChange = (int)(DateTime.UtcNow - lastPasswordChangeDate).TotalDays;
            // Password expires after 90 days
            int passwordExpiryDays = 90;
            daysRemaining = passwordExpiryDays - daysSinceLastChange;

            // Check if password has expired
            if (daysRemaining <= 0)
            {
                // Password has expired
                return RedirectToAction("ResetPassword", new { username = model.Username, expired = true });
            }

            // Add claims values to use in views, when signing in:
            List<Claim> claims = await addExtraClaimsValues(user);

            // Everything is ok, then sign in user
            var result = await _userService.SignInAsync(
                    user, false, claims);

            if (result.Succeeded)
            {
                // Check first login - in case was yes: navigate into change password 
                if ((bool)user.FirstLogin)
                {
                    var token = await _userService.GeneratePasswordResetTokenAsync(user);
                    return RedirectToAction("ResetPassword", new { username = model.Username, expired = true });
                }

                // Check if password will expire soon (within 10 days)
                // To Do: show message to user in Layout view
                if (daysRemaining <= 10)
                    // Store the warning in session so it can be displayed on the next page
                    HttpContext.Session.SetString("passwordExpire", $"ستنتهي صلاحية كلمة المرور بعد {daysRemaining} ايام، نرجو إعادة تعيين كلمة المرور والا سيتم تعطيل الحساب");

                //Log every login action in UserLoginLog table
                await _userService.CreateUserLoginLogAsync(new UserLoginLogDto()
                {
                    UserId = user.Id,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    LogginDateTime = DateTime.UtcNow
                });

                // Login successful, Navigate into Home page 
                HttpContext.Session.SetString("name", user.Name);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("CaptchaInput", "حدث خطأ نرجو المحاولة مرة أخرى!");
            return View(model);
        }
        catch (Exception)
        {
            ModelState.AddModelError("CaptchaInput", "حدث خطأ نرجو المحاولة مرة أخرى!");
            return View(model);
        }

    }

    private async Task<List<Claim>> addExtraClaimsValues(User user)
    {
        var userRoles = await _userService.GetUserRoles(user.Id);
        var firstUserRole = userRoles?.FirstOrDefault();
        
        System.Diagnostics.Debug.WriteLine($"=== Adding Claims for User: {user.UserName} ===");
        System.Diagnostics.Debug.WriteLine($"User Roles Count: {userRoles?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Department ID: {firstUserRole?.DepartmentId.ToString() ?? "NULL"}");
        
        var claims = new List<Claim>
                    {
                        // Standard claims
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(ClaimTypes.Email, user.Email),

                        // Custom claims                        
                        new Claim("name", user.Name ?? string.Empty),
                        new Claim("deptId", firstUserRole?.DepartmentId.ToString() ?? string.Empty),
                        new Claim("roles", userRoles != null && userRoles.Count > 0
                            ? string.Join(",", userRoles.Select(static r => r.ToString()))
                            : string.Empty),
                    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        return claims;
    }

    public IActionResult Captcha()
    {
        var captchaBytes = CaptchaService.GenerateCaptcha(out string captchaText);

        // Store CAPTCHA in session
        HttpContext.Session.SetString("CaptchaCode", captchaText);

        return File(captchaBytes, "image/png");
    }
    #endregion

    #region Reset Password
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string username = null, bool expired = false, bool adminResetUserPassword = false)
    {
        var user = await _userService.FindByNameAsync(username);
        var token = await _userService.GeneratePasswordResetTokenAsync(user);
        var model = new ResetPasswordDto
        {
            Token = token,
            Username = username,
            IsExpired = expired,
            AdminResetUserPassword = adminResetUserPassword
        };

        if (adminResetUserPassword)
            model.CurrentPassword = _encryptionService.Encrypt("123456");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
    {
        // Remove the early ModelState validation check to allow custom validation
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userService.FindByNameAsync(model.Username);
        if (user == null)
        {
            ModelState.AddModelError("error", "حدث خطأ يرجى التواصل مع مدير النظام");
            return View(model);
        }

        // Validate current password first
        var isCurrentPasswordValid = await _userService.CheckPasswordAsync(user, model.CurrentPassword);
        if (!isCurrentPasswordValid && !model.AdminResetUserPassword)
        {
            ModelState.AddModelError("CurrentPassword", "كلمة المرور الحالية غير مطابقة");
            return View(model);
        }

        // Validate that the new password is different from current            
        if (model.CurrentPassword == model.NewPassword && !model.AdminResetUserPassword)
        {
            ModelState.AddModelError("NewPassword", "كلمة المرور الجديدة يجب ان تكون مختلفة عن القديمة");
            return View(model);
        }

        // To Do: check also last 3 password must not be same
        if (await _userService.IsPasswordInRecentHistoryAsync(user.Id, model.CurrentPassword, model.NewPassword, 3) && !model.AdminResetUserPassword)
        {
            ModelState.AddModelError("NewPassword", "كلمة المرور الجديدة يجب ان تكون مختلفة عن اخر 3 كلمات مرور مستخدمة من قبل");
            return View(model);
        }
        // Change the password
        var result = await _userService.ChangePasswordAsync(user, model);

        if (result.Succeeded)
        {
            // Log the password change
            await _unitOfWork.Repository<UserPasswordLog>().AddAsync(new UserPasswordLog
            {
                UserId = user.Id,
                PasswordChange = DateTime.UtcNow,
                OldPassword = _encryptionService.Encrypt(model.CurrentPassword),
                NewPassword = _encryptionService.Encrypt(model.NewPassword),
                CreatedById = user.Id,
                // ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _unitOfWork.Complete(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());

            // update FirstLogin in User table
            user.FirstLogin = false;
            await _userService.UpdateUserAsync(user);

            ViewBag.passwordChanged = "true";

            if (model.AdminResetUserPassword)
            {
                ModelState.AddModelError("success", "تم تعديل كلمة المرور بنجاح");
                return View(model);
            }

            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            // Convert English validation errors to Arabic
            string arabicMessage = error.Code switch
            {
                "PasswordTooShort" => "كلمة المرور يجب أن تكون على الأقل 6 أحرف",
                "PasswordRequiresDigit" => "كلمة المرور يجب أن تحتوي على رقم واحد على الأقل",
                "PasswordRequiresLower" => "كلمة المرور يجب أن تحتوي على حرف صغير واحد على الأقل",
                "PasswordRequiresUpper" => "كلمة المرور يجب أن تحتوي على حرف كبير واحد على الأقل",
                "PasswordRequiresNonAlphanumeric" => "كلمة المرور يجب أن تحتوي على رمز خاص واحد على الأقل (!@#$%^&*)",
                "PasswordMismatch" => "كلمة المرور غير متطابقة",
                "InvalidToken" => "رمز إعادة تعيين كلمة المرور غير صحيح",
                _ => error.Description // Keep original message for unknown errors
            };

            ModelState.AddModelError("NewPassword", arabicMessage);
        }

        return View(model);
    }
    #endregion

    #region Logout
    public async Task<IActionResult> Logout()
    {
        var user = HttpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        // Log every logout action in UserLoginLog table
        await _userService.CreateUserLoginLogAsync(new UserLoginLogDto()
        {
            UserId = userId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ActivityType = "Logout",
            Description = "Logout",
            LoggedOutDateTime = DateTime.UtcNow
        });

        string cacheKey = $"UserPermissions_{userId}";
        await _cache.RemoveAsync(cacheKey);

        HttpContext.Session.Clear();
        await _userService.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }
    #endregion

    #region Account Details
    public async Task<IActionResult> AccountDetails()
    {
        try
        {
            // Get current user ID
            var currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login");
            }

            // Get user information
            var user = await _userService.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Get user roles
            var userRoles = await _userService.GetUserRoles(currentUserId);
            var userRole = userRoles?.FirstOrDefault();

            // Get role information using RoleId
            string roleName = null;
            string roleNameArabic = null;
            if (userRole != null)
            {
                var role = await _roleService.FindByIdAsync(userRole.RoleId);
                roleName = role?.Name;
                roleNameArabic = role?.RoleNameArabic;
            }

            // Get department information
            var departmentName = userRole?.Department?.Name;
            var departmentTypeName = userRole?.Department?.DepartmentType?.DepartmentTypeName;

            // Get last login date from UserLoginLog
            var userLoginLogs = await _unitOfWork.Repository<UserLoginLog>()
                .GetAsync(log => log.UserId == currentUserId && log.ActivityType == "Login");

            var lastLoginLog = userLoginLogs
                .OrderByDescending(log => log.LogginDateTime)
                .FirstOrDefault();

            // Create view model with all user information
            var accountDetailsModel = new
            {
                UserId = user.Id,
                Username = user.UserName,
                FullName = user.Name,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                CreatedDate = user.CreatedDate,
                LastLoginDate = lastLoginLog?.LogginDateTime,
                RoleName = roleName,
                RoleNameArabic = roleNameArabic,
                DepartmentName = departmentName ?? "غير محدد",
                DepartmentTypeName = departmentTypeName ?? "غير محدد"
            };

            ViewData["Title"] = "بيانات الحساب";
            return View(accountDetailsModel);
        }
        catch (Exception ex)
        {
            // Log error and redirect to home
            return RedirectToAction("Index", "Home");
        }
    }
    #endregion
}

