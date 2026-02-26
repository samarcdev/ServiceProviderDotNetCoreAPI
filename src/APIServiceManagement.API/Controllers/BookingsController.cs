using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using APIServiceManagement.API.Attributes;
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
        var customerId = GetUserId(); // Returns null if not authenticated
        var result = await _bookingService.GetAllAvailableServicesAsync(customerId, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("top-services")]
    public async Task<IActionResult> GetTopBookedServices(
        [FromQuery] int days = 90,
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetUserId(); // Returns null if not authenticated
        var result = await _bookingService.GetTopBookedServicesAsync(days, limit, customerId, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("available-services-by-pincode")]
    public async Task<IActionResult> GetAvailableServicesByPincode([FromQuery] string pincode, CancellationToken cancellationToken)
    {
        var customerId = GetUserId(); // Returns null if not authenticated
        var result = await _bookingService.GetAvailableServicesByPincodeAsync(pincode, customerId, cancellationToken);
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

    [AllowAnonymous]
    [HttpGet("service-types")]
    public async Task<IActionResult> GetServiceTypes(
        [FromQuery] int? serviceId,
        CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetServiceTypesAsync(serviceId, cancellationToken);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpGet("time-slots")]
    public async Task<IActionResult> GetTimeSlots(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetAvailableTimeSlotsAsync(date, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeCustomer]
    [HttpGet("summary")]
    public async Task<IActionResult> GetBookingSummary(
        [FromQuery] int serviceId,
        [FromQuery] int? serviceTypeId,
        [FromQuery] string pincode,
        [FromQuery] DateTime? preferredDate,
        [FromQuery] string? timeSlot,
        [FromQuery] string? discountCode,
        CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetBookingSummaryAsync(
            GetUserId(),
            serviceId,
            serviceTypeId,
            pincode,
            preferredDate,
            timeSlot,
            discountCode,
            cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeCustomer]
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
        [FromQuery] string? search = null,
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
            search,
            cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeAdmin]
    [HttpGet("available-providers")]
    public async Task<IActionResult> GetAvailableServiceProviders(
        [FromQuery] int serviceId,
        [FromQuery] string pincode,
        [FromQuery] DateTime? preferredDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookingService.GetAvailableServiceProvidersAsync(serviceId, pincode, preferredDate, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeAdmin]
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

    [Authorize(Roles = "Customer,ServiceProvider")]
    [HttpGet("pincode-preferences")]
    public async Task<IActionResult> GetUserPincodePreferences(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetUserPincodePreferencesAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Customer,ServiceProvider")]
    [HttpPost("pincode-preferences")]
    public async Task<IActionResult> SaveUserPincodePreference([FromBody] PincodePreferenceRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookingService.SaveUserPincodePreferenceAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Customer,ServiceProvider")]
    [HttpDelete("pincode-preferences/{preferenceId:guid}")]
    public async Task<IActionResult> DeleteUserPincodePreference(Guid preferenceId, CancellationToken cancellationToken)
    {
        var result = await _bookingService.DeleteUserPincodePreferenceAsync(GetUserId(), preferenceId, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeCustomer]
    [HttpPost("{bookingId:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken cancellationToken)
    {
        var result = await _bookingService.CancelBookingAsync(GetUserId(), bookingId, cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
