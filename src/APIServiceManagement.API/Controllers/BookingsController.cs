using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [AllowAnonymous]
    [HttpGet("available-services")]
    public async Task<IActionResult> GetAllAvailableServices(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetAllAvailableServicesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("available-services-by-pincode")]
    public async Task<IActionResult> GetAvailableServicesByPincode([FromQuery] string pincode, CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetAvailableServicesByPincodeAsync(pincode, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("validate-pincode")]
    public async Task<IActionResult> ValidatePincode([FromQuery] string pincode, CancellationToken cancellationToken)
    {
        var result = await _bookingService.ValidatePincodeAsync(pincode, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("calculate-price")]
    public async Task<IActionResult> CalculateServicePrice(
        [FromQuery] int serviceId,
        [FromQuery] string pincode,
        CancellationToken cancellationToken)
    {
        var result = await _bookingService.CalculateServicePriceAsync(serviceId, pincode, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] BookingCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CreateBookingAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetBookingRequests(
        [FromQuery] string? status,
        [FromQuery] string? pincode,
        [FromQuery] int? serviceId,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? serviceProviderId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.GetBookingRequestsAsync(
            status,
            pincode,
            serviceId,
            customerId,
            serviceProviderId,
            dateFrom,
            dateTo,
            sortBy,
            sortOrder,
            page,
            limit,
            cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("assign")]
    public async Task<IActionResult> AssignServiceProvider([FromBody] BookingAssignmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.AssignServiceProviderAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPatch("{bookingId:guid}")]
    public async Task<IActionResult> UpdateBookingStatus(Guid bookingId, [FromBody] BookingUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.UpdateBookingStatusAsync(bookingId, request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("pincode-preferences")]
    public async Task<IActionResult> GetUserPincodePreferences(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetUserPincodePreferencesAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("pincode-preferences")]
    public async Task<IActionResult> SaveUserPincodePreference([FromBody] PincodePreferenceRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.SaveUserPincodePreferenceAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
