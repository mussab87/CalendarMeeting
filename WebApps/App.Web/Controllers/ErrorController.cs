using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

