using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers;

[Authorize(Roles = "Owner,Worker")]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(OrderApprovalRequest request, CancellationToken cancellationToken)
    {
        await _orderService.ApproveOrderAsync(
            request.OrderId,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(OrderRejectionRequest request, CancellationToken cancellationToken)
    {
        await _orderService.RejectOrderAsync(
            request.OrderId,
            request.RejectionReason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
