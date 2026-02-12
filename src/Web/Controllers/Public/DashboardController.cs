using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    [Authorize(Roles = "Owner")]
    public IActionResult Owner()
    {
        return View();
    }

    [Authorize(Roles = "Worker")]
    public IActionResult Worker()
    {
        return View();
    }

    [Authorize(Roles = "Client")]
    public IActionResult Client()
    {
        return View();
    }
}
