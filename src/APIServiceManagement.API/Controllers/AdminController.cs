using APIServiceManagement.API.Attributes;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuthorizeAdmin] // Requires Admin, MasterAdmin, or DefaultAdmin role
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetDashboardStatsAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("verifications")]
    public async Task<IActionResult> GetVerificationsByStatus(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetVerificationsByStatusAsync(GetUserId(), status, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("verifications/{verificationId}")]
    public async Task<IActionResult> GetVerificationDetails(
        int verificationId,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.GetVerificationDetailsAsync(verificationId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("verifications/{verificationId}/status")]
    public async Task<IActionResult> UpdateVerificationStatus(
        int verificationId,
        [FromBody] UpdateVerificationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateVerificationStatusAsync(GetUserId(), verificationId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> CheckAdminPermissions(CancellationToken cancellationToken)
    {
        var result = await _adminService.CheckAdminPermissionsAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("assigned-states")]
    public async Task<IActionResult> GetAssignedStates(CancellationToken cancellationToken)
    {
        var result = await _adminService.GetAdminAssignedStatesAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
