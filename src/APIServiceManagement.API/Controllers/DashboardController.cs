using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public DashboardController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize]
    [HttpGet("customer")]
    public async Task<IActionResult> GetCustomerDashboard(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetCustomerDashboardAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("admin")]
    public async Task<IActionResult> GetAdminDashboard(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetAdminDashboardAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("service-provider")]
    public async Task<IActionResult> GetServiceProviderDashboard(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetServiceProviderDashboardAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
