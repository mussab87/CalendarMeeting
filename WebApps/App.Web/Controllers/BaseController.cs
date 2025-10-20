using App.Application.Contracts.Repositories.IUserService;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;
public class BaseController : Controller
{
    protected readonly IUserService _userService;
    protected readonly IRoleService _roleService;
    protected readonly IConfiguration _config;
    protected readonly ICacheService _cache;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IEncryptionService _encryptionService;

    // public readonly IMediator _mediator;
    protected readonly IMapper _mapper;

    public BaseController(IServiceProvider serviceProvider)
    {
        this._userService = serviceProvider.GetRequiredService<IUserService>();
        _roleService = serviceProvider.GetRequiredService<IRoleService>();
        _config = serviceProvider.GetRequiredService<IConfiguration>();
        _cache = serviceProvider.GetRequiredService<ICacheService>();
        _unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        _encryptionService = serviceProvider.GetRequiredService<IEncryptionService>();
        _mapper = serviceProvider.GetRequiredService<IMapper>();
        //_roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        //_mediator = serviceProvider.GetRequiredService<IMediator>();        
    }
}