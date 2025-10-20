using App.Application.Contracts.Repositories.IUserService;
using App.Domain.UserSecurity;
using App.Infrastructure.UserSecurity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure { }

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEntityFrameworkSqlServer();

        //Register Database Context
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(@"Data Source=MUSSAB\SQLEXPRESS;Initial Catalog=CalendarMeetingDB;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False",
                                    x => x.MigrationsAssembly("App.Infrastructure"))
                   .LogTo(Console.WriteLine, LogLevel.Information));

        //Register Identity Services (Only Once!)
        services.AddIdentity<User, Role>(options =>
        {
            var appSettings = configuration.GetSection("AppSetting");
            options.Password.RequireDigit = appSettings.GetValue("PassRequireDigit", true);
            options.Password.RequireLowercase = appSettings.GetValue("PassRequireLowercase", true);
            options.Password.RequireUppercase = appSettings.GetValue("PassRequireUppercase", true);
            options.Password.RequireNonAlphanumeric = appSettings.GetValue("PassRequireNonAlphanumeric", true);
            options.Password.RequiredLength = appSettings.GetValue("UserPasswordLength", 6);

            //Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            //User settings
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<Role>()
            .AddRoleManager<RoleManager<Role>>()
            .AddSignInManager<SignInManager<User>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

        //Configure Authentication & Cookies
        services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(configuration.GetSection("AppSetting").GetValue<int>("UserSessionTimeOut", 30));
            options.LoginPath = "/Account/Login";
            options.SlidingExpiration = true;
        });

        //Register Role & User Services
        services.AddScoped<IRoleService, RoleService>(); // Register RoleService
        services.AddScoped<IUserService, UserService>(); // Register UserService

        //Register DbInitializer AFTER Identity Services - Database Seeder (Optional)
        services.AddScoped<IDbInitializer, DbInitializer>();

        //Register UnitOfWork & GenericRepository Services
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        //Configure Security Stamp Validator
        services.Configure<SecurityStampValidatorOptions>(options =>
        {
            options.ValidationInterval = TimeSpan.Zero;
        });

        // Register the cache service
        services.AddDistributedMemoryCache();
        services.AddScoped<ICacheService, DistributedCacheService>();
        services.AddHttpContextAccessor();
        services.AddScoped<PermissionAuthorizationFilter>(); // Register filter for DI
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddScoped<IMeetingService, MeetingService>();
        services.AddScoped<INotificationService, NotificationService>();


        return services;
    }
}

