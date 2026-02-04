using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Client")]
public class ClientController : Controller
{
    public IActionResult MyOrders()
    {
        return View();
    }

    public IActionResult NewOrder()
    {
        return View();
    }

    public IActionResult Profile()
    {
        return View();
    }
}
