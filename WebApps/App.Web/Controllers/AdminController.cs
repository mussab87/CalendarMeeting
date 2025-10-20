using App.Domain.UserSecurity;
using App.Helper.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using X.PagedList.Extensions;

namespace App.Web.Controllers;

[PermissionAuthorize]
public class AdminController : BaseController
{
    private readonly ILogger<AdminController> _logger;
    private readonly UserManager<User> _userManager;

    public AdminController(IServiceProvider serviceProvider, ILogger<AdminController> logger) : base(serviceProvider)
    {
        _logger = logger;
        _userManager = serviceProvider.GetRequiredService<UserManager<User>>();
    }

    #region Get All Users
    public async Task<IActionResult> GetUsers(string searchString = "", int page = 1, int pageSize = 10)
    {
        // Get current user ID
        var currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Check if current user is Super Admin
        var currentUserRoles = await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(currentUserId));
        var isSuperAdmin = currentUserRoles.Contains(Roles.SuperAdmin);

        // Pass filter parameter: true = show all users (Super Admin), false = exclude Super Admin users
        var allUsers = await _userService.GetPaginatedUsers(page, pageSize, searchString, 1, "asc", isSuperAdmin);

        return View(allUsers);
    }

    #endregion

    #region Add Edit User
    public async Task<IActionResult> AddEditUser(int? actionType, string userId)
    {
        UserDto userDtoModel = null;
        try
        {
            // Get current user ID and check if they are Super Admin
            var currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRoles = await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(currentUserId));
            var isCurrentUserSuperAdmin = currentUserRoles.Contains(Roles.SuperAdmin);

            var roles = await _roleService.GetAllRolesAsync();

            // Filter roles based on current user's role: Super Admin sees all roles, others don't see Super Admin role
            var filteredRoles = roles.Where(r => r.IsDeleted == false);
            if (!isCurrentUserSuperAdmin)
            {
                // Exclude Super Admin role for non-Super Admin users
                filteredRoles = filteredRoles.Where(r => r.Name != Roles.SuperAdmin);
            }

            ViewData["roles"] = new SelectList(filteredRoles, "Id", "RoleNameArabic");
            ViewData["deptType"] = new SelectList(await _unitOfWork.Repository<DepartmentType>().GetAllAsync(), "Id", "DepartmentTypeName");

            // Default: empty departments
            IEnumerable<Department> departments = Enumerable.Empty<Department>();
            int? selectedDepartmentId = null;

            if (actionType == (int)ActionTypeEnum.Add)
            {
                userDtoModel = new UserDto();
            }
            else if (actionType == (int)ActionTypeEnum.Update)
            {
                userDtoModel = _mapper.Map<UserDto>(await _userService.GetUserByIdAsync(userId));
                if (userDtoModel == null)
                    return PartialView("AddEditUser", userDtoModel);

                userDtoModel.ActionType = 1;
                userDtoModel.Id = userId;

                // Get user roles and department info
                var userRoles = await _userService.GetUserRoles(userId);
                var firstUserRole = userRoles.FirstOrDefault();

                if (firstUserRole != null)
                {
                    userDtoModel.RoleId = firstUserRole.RoleId;

                    // Ensure DepartmentTypeId and DepartmentId are set
                    if (userDtoModel.DepartmentTypeId == null)
                        userDtoModel.DepartmentTypeId = firstUserRole.Department?.DepartmentTypeId;

                    if (userDtoModel.DepartmentId == null)
                        userDtoModel.DepartmentId = firstUserRole.Department?.Id;

                    // Populate departments for the selected DepartmentTypeId
                    if (userDtoModel.DepartmentTypeId != null)
                    {
                        departments = await _unitOfWork.Repository<Department>().GetAsync(d => d.DepartmentTypeId == userDtoModel.DepartmentTypeId);
                        selectedDepartmentId = userDtoModel.DepartmentId;
                    }
                }
            }

            ViewData["departments"] = new SelectList(departments, "Id", "Name", selectedDepartmentId);

            return PartialView("AddEditUser", userDtoModel);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ!" + ex.ToString());
            return PartialView("AddEditUser", userDtoModel);
        }

    }

    [HttpPost]
    public async Task<IActionResult> AddEditUser(UserDto model)
    {
        try
        {
            // Get current user ID and check if they are Super Admin
            var currentUserId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserRoles = await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(currentUserId));
            var isCurrentUserSuperAdmin = currentUserRoles.Contains(Roles.SuperAdmin);

            var roles = await _roleService.GetAllRolesAsync();

            // Filter roles based on current user's role: Super Admin sees all roles, others don't see Super Admin role
            var filteredRoles = roles.Where(r => r.IsDeleted == false);
            if (!isCurrentUserSuperAdmin)
            {
                // Exclude Super Admin role for non-Super Admin users
                filteredRoles = filteredRoles.Where(r => r.Name != Roles.SuperAdmin);
            }

            ViewData["roles"] = new SelectList(filteredRoles, "Id", "RoleNameArabic");
            ViewData["deptType"] = new SelectList(await _unitOfWork.Repository<DepartmentType>().GetAllAsync(), "Id", "DepartmentTypeName");

            // Repopulate departments if DepartmentTypeId is selected
            if (model.DepartmentTypeId != null)
            {
                var departments = await _unitOfWork.Repository<Department>().GetAsync(d => d.DepartmentTypeId == model.DepartmentTypeId);
                ViewData["departments"] = new SelectList(departments, "Id", "Name");
            }
            else
            {
                ViewData["departments"] = new SelectList(Enumerable.Empty<Department>(), "Id", "Name");
            }

            //Add new user
            if (model.ActionType == 0)
            {
                if (!ModelState.IsValid)
                    return PartialView("AddEditUser", model);

                model.CreatedBy = this.User.Identity.Name;

                // get selected role to Add, set default password th user for first login
                var selectedRole = filteredRoles.FirstOrDefault(r => r.Id == model.RoleId);

                // Add new user
                await this._userService.CreateUser(model, "Aa@123456", selectedRole.Name, (int)model.DepartmentId);

                return Ok(new { success = true, data = "تم الحفظ بنجاح" });
            }

            //Update Exist user
            if (model.ActionType == 1)
            {
                if (!ModelState.IsValid)
                    return PartialView("AddEditUser", model);

                model.LastModifiedBy = User.Identity.Name;
                //update user
                await _userService.UpdateUserAsync(model);

                return Ok(new { success = true, data = "تم الحفظ بنجاح" });
            }

            return PartialView("AddEditUser", model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ اثناء حفظ البيانات !" + ex.ToString());
            return PartialView("AddEditUser", model);
        }
    }

    #endregion

    #region Delete user
    [HttpPost]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = true, data = "هذا المستخدم غير موجود حاليا، فضلا تواصل مع الدعم الفني" });

            //update user IsDeleted
            user.IsDeleted = true;
            user.IsActive = false;
            var result = await _userService.UpdateUserAsync(user);
            return Json(new { success = true, data = "تم الحفظ بنجاح" });

        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ اثناء حفظ البيانات !" + ex.ToString());
            return Json(new { success = false, data = "حدث خطأ اثناء حفظ البانات، فضلا تواصل مع الدعم الفني" + ex.ToString() });
        }
    }
    #endregion

    #region Get All Roles
    public async Task<IActionResult> GetRoles(string searchString = "", int page = 1, int pageSize = 10)
    {
        var allUsers = await _roleService.GetPaginatedRoles(page, pageSize, searchString);

        return View(allUsers);
    }

    #endregion

    #region Add Edit Role
    public async Task<IActionResult> AddEditRole(int? actionType, string roleId)
    {
        RoleDto roleDtoModel = null;
        try
        {
            var roles = await _roleService.GetAllRolesAsync();
            //ViewData["roles"] = new SelectList(roles, "Id", "RoleNameArabic");

            if (actionType == 0)
                roleDtoModel = new RoleDto();

            if (actionType == 1)
            {
                roleDtoModel = _mapper.Map<RoleDto>(await _roleService.FindByIdAsync(roleId));
                if (roleDtoModel == null)
                {
                    return PartialView("AddEditRole", roleDtoModel);
                }
                roleDtoModel.ActionType = 1;
                roleDtoModel.Id = roleId;
            }
            return PartialView("AddEditRole", roleDtoModel);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ!" + ex.ToString());
            return PartialView("AddEditRole", roleDtoModel);
        }

    }

    [HttpPost]
    public async Task<IActionResult> AddEditRole(RoleDto model)
    {
        try
        {
            //Add new role
            if (model.ActionType == 0)
            {
                if (!ModelState.IsValid)
                    return PartialView("AddEditRole", model);

                model.CreatedById = User.Identity.Name;
                //Add new role
                await _roleService.CreateRoleAsync(model);

                return Ok(new { success = true, data = "تم الحفظ بنجاح" });
            }

            //Update Exist role
            if (model.ActionType == 1)
            {
                if (!ModelState.IsValid)
                    return PartialView("AddEditRole", model);

                model.LastModifiedById = User.Identity.Name;
                //update user
                await _roleService.UpdateRoleAsync(model);

                return Ok(new { success = true, data = "تم الحفظ بنجاح" });
            }

            return PartialView("AddEditRole", model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ اثناء حفظ البيانات !" + ex.ToString());
            return PartialView("AddEditRole", model);
        }
    }
    #endregion

    #region Delete Role
    [HttpPost]
    public async Task<IActionResult> DeleteRole(string roleId)
    {
        try
        {
            var role = await _roleService.FindByIdAsync(roleId);
            if (role == null)
                return Json(new { success = true, data = "هذه الصلاحية غير موجودة حاليا، فضلا تواصل مع الدعم الفني" });

            //update role IsDeleted
            role.IsDeleted = true;
            var result = await _roleService.UpdateRoleAsync(_mapper.Map<RoleDto>(role));
            return Json(new { success = true, data = "تم الحفظ بنجاح" });

        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ اثناء حفظ البيانات !" + ex.ToString());
            return Json(new { success = false, data = "حدث خطأ اثناء حفظ البانات، فضلا تواصل مع الدعم الفني" + ex.ToString() });
        }
    }
    #endregion

    #region Add Claims (permossions) into Role
    public async Task<IActionResult> AddPermissions(string roleId)
    {
        try
        {
            return PartialView("AddPermissions", await _roleService.GetClaimsAddPermissionseAsync(roleId));
        }
        catch (Exception ex)
        {
            return Json(new { success = false, data = "حدث خطأ اثناء حفظ البانات، فضلا تواصل مع الدعم الفني" + ex.ToString() });
        }

    }

    [HttpPost]
    public async Task<IActionResult> AddPermissions(RoleClaimsDto model)
    {
        try
        {
            await _roleService.AddClaimsToRole(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, model);

            TempData["Success"] = "تم حفظ البيانات بنجاح";
            return PartialView("AddPermissions", model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "حدث خطأ اثناء حفظ البيانات !" + ex.ToString());
            return PartialView("AddPermissions", model);
        }
    }
    #endregion



    #region Get Departments By Type
    public async Task<IActionResult> GetDepartmentsByType(int departmentTypeId)
    {
        var departments = await _unitOfWork.Repository<Department>()
            .GetAsync(d => d.DepartmentTypeId == departmentTypeId);

        var result = departments
            .Select(d => new { d.Id, d.Name })
            .ToList();

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartmentTypes()
    {
        try
        {
            var departmentTypes = await _unitOfWork.Repository<DepartmentType>()
                .GetAllAsync();

            var result = departmentTypes
                .Select(dt => new { id = dt.Id, name = dt.DepartmentTypeName })
                .ToList();

            return Json(result);
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error in GetDepartmentTypes: {ex.Message}");
            return Json(new List<object>());
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDepartment(int DepartmentTypeId, string DepartmentName)
    {
        try
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Validate inputs
            if (DepartmentTypeId <= 0)
            {
                return Json(new { success = false, message = "نوع القسم غير صحيح" });
            }

            if (string.IsNullOrWhiteSpace(DepartmentName))
            {
                return Json(new { success = false, message = "اسم القسم مطلوب" });
            }

            // Check if department name already exists for this type
            var existingDepartment = await _unitOfWork.Repository<Department>()
                .GetAsync(d => d.DepartmentTypeId == DepartmentTypeId &&
                             d.Name.ToLower() == DepartmentName.ToLower() &&
                             !d.IsDeleted);

            if (existingDepartment.Any())
            {
                return Json(new { success = false, message = "اسم القسم موجود بالفعل لهذا النوع" });
            }

            // Create new department
            var newDepartment = new Department
            {
                DepartmentTypeId = DepartmentTypeId,
                Name = DepartmentName.Trim(),
                Description = DepartmentName,
                IsDeleted = false,
                CreatedById = userId,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Department>().AddAsync(newDepartment);
            await _unitOfWork.Complete(userId);

            return Json(new
            {
                success = true,
                message = "تم إضافة القسم بنجاح",
                departmentId = newDepartment.Id,
                departmentName = newDepartment.Name
            });
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error in CreateDepartment: {ex.Message}");
            return Json(new { success = false, message = "حدث خطأ أثناء حفظ القسم" });
        }
    }
    #endregion

    #region Get All Departments
    public async Task<IActionResult> GetDepartments(string searchString = "", int page = 1, int pageSize = 10)
    {
        try
        {
            // Get all departments with related data
            var allDepartments = await _unitOfWork.Repository<Department>()
                .GetAsync(includeString: "DepartmentType");

            // Manually map to DTOs with related data
            var departmentDtos = new List<DepartmentDto>();
            foreach (var department in allDepartments)
            {
                var departmentDto = new DepartmentDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    Description = department.Description,
                    DepartmentTypeId = department.DepartmentTypeId,
                    DepartmentTypeName = department.DepartmentType?.DepartmentTypeName,
                    IsDeleted = department.IsDeleted,
                    CreatedById = department.CreatedById,
                    CreatedDate = department.CreatedDate,
                    LastModifiedById = department.LastModifiedById,
                    LastModifiedDate = department.LastModifiedDate
                };
                departmentDtos.Add(departmentDto);
            }

            // Apply search filtering
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.ToLower();
                departmentDtos = departmentDtos.Where(d =>
                    (d.Name?.ToLower().Contains(searchString) ?? false) ||
                    (d.Description?.ToLower().Contains(searchString) ?? false) ||
                    (d.DepartmentTypeName?.ToLower().Contains(searchString) ?? false)
                ).ToList();
            }

            var totalCount = departmentDtos.Count;

            return View(new PaginatedResult<DepartmentDto>
            {
                Items = departmentDtos.ToPagedList<DepartmentDto>(page, pageSize),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize,
                SearchString = searchString
            });
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error in GetDepartments: {ex.Message}");
            return View(new PaginatedResult<DepartmentDto>
            {
                Items = new List<DepartmentDto>().ToPagedList<DepartmentDto>(1, 10),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 10,
                SearchString = searchString
            });
        }
    }
    #endregion

    #region Add Edit Department
    public async Task<IActionResult> AddEditDepartment(int? actionType, int? departmentId)
    {
        DepartmentDto departmentDtoModel = null;
        try
        {
            var departmentTypes = await _unitOfWork.Repository<DepartmentType>().GetAllAsync();
            ViewData["departmentTypes"] = new SelectList(departmentTypes, "Id", "DepartmentTypeName");

            if (actionType == (int)ActionTypeEnum.Add)
            {
                departmentDtoModel = new DepartmentDto();
            }
            else if (actionType == (int)ActionTypeEnum.Update && departmentId.HasValue)
            {
                var departments = await _unitOfWork.Repository<Department>()
                    .GetAsync(predicate: d => d.Id == departmentId.Value, includeString: "DepartmentType");

                var department = departments?.FirstOrDefault();
                if (department != null)
                {
                    departmentDtoModel = new DepartmentDto
                    {
                        Id = department.Id,
                        Name = department.Name,
                        Description = department.Description,
                        DepartmentTypeId = department.DepartmentTypeId,
                        DepartmentTypeName = department.DepartmentType?.DepartmentTypeName,
                        IsDeleted = department.IsDeleted,
                        CreatedById = department.CreatedById,
                        CreatedDate = department.CreatedDate,
                        LastModifiedById = department.LastModifiedById,
                        LastModifiedDate = department.LastModifiedDate,
                        ActionType = (int)ActionTypeEnum.Update
                    };
                }
            }

            if (departmentDtoModel == null)
            {
                departmentDtoModel = new DepartmentDto();
            }

            return PartialView("AddEditDepartment", departmentDtoModel);
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error in AddEditDepartment: {ex.Message}");
            return PartialView("AddEditDepartment", new DepartmentDto());
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddEditDepartment(DepartmentDto departmentDto)
    {
        try
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (departmentDto.ActionType == (int)ActionTypeEnum.Add)
            {
                var newDepartment = new Department
                {
                    Name = departmentDto.Name,
                    Description = departmentDto.Description,
                    DepartmentTypeId = departmentDto.DepartmentTypeId,
                    IsDeleted = false,
                    CreatedById = userId,
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Department>().AddAsync(newDepartment);
                await _unitOfWork.Complete(userId);

                return Json(new { success = true, message = "تم إضافة القسم بنجاح", data = "تم إضافة القسم بنجاح" });
            }
            else if (departmentDto.ActionType == (int)ActionTypeEnum.Update)
            {
                var existingDepartments = await _unitOfWork.Repository<Department>()
                    .GetAsync(d => d.Id == departmentDto.Id);

                var existingDepartment = existingDepartments?.FirstOrDefault();
                if (existingDepartment != null)
                {
                    existingDepartment.Name = departmentDto.Name;
                    existingDepartment.Description = departmentDto.Description;
                    existingDepartment.DepartmentTypeId = departmentDto.DepartmentTypeId;
                    existingDepartment.LastModifiedById = userId;
                    existingDepartment.LastModifiedDate = DateTime.UtcNow;

                    await _unitOfWork.Repository<Department>().UpdateAsync(existingDepartment);
                    await _unitOfWork.Complete(userId);

                    return Json(new { success = true, message = "تم تحديث القسم بنجاح", data = "تم تحديث القسم بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "القسم غير موجود" });
                }
            }

            return Json(new { success = false, message = "نوع العملية غير صحيح" });
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error in AddEditDepartment POST: {ex.Message}");
            return Json(new { success = false, message = "حدث خطأ أثناء حفظ القسم" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDepartment(int departmentId)
    {
        try
        {
            var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var departments = await _unitOfWork.Repository<Department>()
                .GetAsync(d => d.Id == departmentId);

            var department = departments?.FirstOrDefault();
            if (department != null)
            {
                department.IsDeleted = true;
                department.LastModifiedById = userId;
                department.LastModifiedDate = DateTime.UtcNow;

                await _unitOfWork.Repository<Department>().UpdateAsync(department);
                await _unitOfWork.Complete(userId);

                return Json(new { success = true, message = "تم حذف القسم بنجاح", data = "تم حذف القسم بنجاح" });
            }
            else
            {
                return Json(new { success = false, message = "القسم غير موجود" });
            }
        }
        catch (Exception ex)
        {
            //System.Diagnostics.Debug.WriteLine($"Error in DeleteDepartment: {ex.Message}");
            return Json(new { success = false, message = "حدث خطأ أثناء حذف القسم" });
        }
    }
    #endregion
}
