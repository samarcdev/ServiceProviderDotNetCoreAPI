using APIServiceManagement.API.Attributes;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeavesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [AuthorizeServiceProvider]
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyLeave(
        [FromBody] ApplyLeaveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _leaveService.ApplyLeaveAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpGet]
    public async Task<IActionResult> GetLeaves(CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetLeavesAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpPost("cancel-day")]
    public async Task<IActionResult> CancelLeaveDay(
        [FromBody] CancelLeaveDayRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _leaveService.CancelLeaveDayAsync(GetUserId(), request.Date, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpGet("bookings-for-reassignment")]
    public async Task<IActionResult> GetBookingsForReassignment(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _leaveService.GetBookingsForReassignmentAsync(
            GetUserId(),
            startDate,
            endDate,
            cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeAdmin]
    [HttpGet("check-leave")]
    public async Task<IActionResult> CheckLeave(
        [FromQuery] Guid serviceProviderId,
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        var result = await _leaveService.IsServiceProviderOnLeaveAsync(serviceProviderId, date, cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
