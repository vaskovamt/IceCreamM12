using System.Security.Claims;
using IceCreamM12.Application.Interfaces;
using IceCreamM12.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IceCreamM12.Web.Controllers.Admin;

[Authorize(Roles = "Owner")]
[Route("Admin/[controller]/[action]")]
public class OwnerController : Controller
{
    private readonly IAuditService _auditService;
    private readonly IInventoryService _inventoryService;

    public OwnerController(IAuditService auditService, IInventoryService inventoryService)
    {
        _auditService = auditService;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View("~/Views/Admin/Owner/Index.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordInventoryChange(InventoryLoadRequest request, CancellationToken cancellationToken)
    {
        var item = await _inventoryService.LoadInventoryAsync(
            request.ProductId,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        await _auditService.RecordInventoryChangeAsync(
            item,
            request.Quantity,
            request.Reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
