using App.Application.Contracts.Repositories.IUserService;
using App.Domain.UserSecurity;
using App.Helper.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure.Initializer { }

public class DbInitializer : IDbInitializer
{
    private AppDbContext _dbContext;
    private IUserService _userService;
    private IRoleService _roleService;
    private readonly IServiceProvider _serviceProvider;

    public DbInitializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            _dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            _roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

            // Apply pending migrations before seeding data
            if (_dbContext.Database.GetPendingMigrations().Any())
            {
                await _dbContext.Database.MigrateAsync();
            }

            if (!this._dbContext.DepartmentTypes.Any())
            {
                //seed data
                await seedDepartment();
            }

            //Check role SuperAdmin exist or no
            if (!await _roleService.RoleExistsAsync(Roles.SuperAdmin))
            {
                //In case SuperAdmin role not Exist
                //system run at first time - Create SuperAdmin role and Admin user
                var roleToAdd = new RoleDto
                {
                    Id = Roles.SuperAdminId,
                    Name = Roles.SuperAdmin,
                    RoleNameArabic = "مدير النظام",
                    CreatedById = "System Super Admin"
                };
                var newRole = await _roleService.CreateRoleAsync(roleToAdd);

                //Create User admin and link with the Role was created a top SuperAdmin
                var adminUser = await CreateAdminUser();

                var role = await _roleService.FindByNameAsync(Roles.SuperAdmin);
                if (role != null)
                {
                    var allPermissions = GetAllClaimsPermissions.GetAllControllerActionsUpdated();
                    //UserHelper userHelper = new(_db, _roleManager);
                    await _roleService.AddClaimsToRole(adminUser, role, allPermissions);
                }
            }
            else
            {
                return;
            }
        }
        catch (Exception)
        {
        }
    }

    private async Task seedDepartment()
    {
        await _dbContext.DepartmentTypes.AddRangeAsync(
                                new DepartmentType()
                                {
                                    DepartmentTypeName = "هيئة",
                                    CreatedDate = DateTime.UtcNow
                                },
                                new DepartmentType()
                                {
                                    DepartmentTypeName = "إدارة",
                                    CreatedDate = DateTime.UtcNow
                                }
                            );
        await _dbContext.SaveChangesAsync();

        await _dbContext.Departments.AddAsync(
            new Department()
            {
                Name = "تقنية المعلومات",
                DepartmentTypeId = _dbContext.DepartmentTypes.FirstOrDefault(x => x.DepartmentTypeName == "هيئة").Id,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            }
        );
        await _dbContext.SaveChangesAsync();
    }

    async Task<User> CreateAdminUser()
    {
        UserDto adminUser = new UserDto()
        {
            Username = "admin",
            Email = "admin@admin.com",
            EmailConfirmed = true,
            PhoneNumber = "111111111111",
            Name = "admin test",
            UserStatus = true,
            FirstLogin = true,
            IsActive = true,
            IsDeleted = false,
            CreatedBy = "System Super Admin"
        };
        return await _userService.CreateUser(adminUser, "Aa@123456", Roles.SuperAdmin, _dbContext.DepartmentTypes.FirstOrDefault(x => x.DepartmentTypeName == "هيئة").Id);
    }
}

