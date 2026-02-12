using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker")]
public class OperationsController : Controller
{
    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Inventory()
    {
        return View();
    }

    public IActionResult Orders()
    {
        return View();
    }

    public IActionResult Production()
    {
        return View();
    }

    public IActionResult DailyInventoryReport()
    {
        return View();
    }
}
