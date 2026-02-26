using APIServiceManagement.API.Attributes;
using APIServiceManagement.API.DTOs.Requests;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceProvidersController : ControllerBase
{
    private readonly IServiceProviderRegistrationService _registrationService;
    private readonly IServiceProviderProfileService _profileService;
    private readonly IBookingService _bookingService;
    private readonly IProviderAvailabilityService _providerAvailabilityService;

    public ServiceProvidersController(
        IServiceProviderRegistrationService registrationService,
        IServiceProviderProfileService profileService,
        IBookingService bookingService,
        IProviderAvailabilityService providerAvailabilityService)
    {
        _registrationService = registrationService;
        _profileService = profileService;
        _bookingService = bookingService;
        _providerAvailabilityService = providerAvailabilityService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] ServiceProviderRegisterFormRequest request, CancellationToken cancellationToken)
    {
        var response = await _registrationService.RegisterAsync(request.ToApplicationRequest(), cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [AuthorizeServiceProvider]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetProfileAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ServiceProviderProfileUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateProfileAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpGet("missing-details")]
    public async Task<IActionResult> GetMissingDetails(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetMissingDetailsAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }
    [AuthorizeServiceProvider]
    [HttpPost("missing-details")]
    public async Task<IActionResult> UpdateMissingDetails([FromBody] ServiceProviderDetailsUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateDetailsAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _bookingService.GetServiceProviderDashboardAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? status,
        [FromQuery] string? pincode,
        [FromQuery] int? serviceId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _bookingService.GetBookingRequestsAsync(
            status,
            pincode,
            serviceId,
            null,
            userId,
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

    [AuthorizeServiceProvider]
    [HttpPost("documents")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadDocument([FromForm] ServiceProviderDocumentUploadRequest request, CancellationToken cancellationToken)
    {
        var fileRequest = new FileUploadRequest
        {
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Length = request.File.Length,
            OpenReadStream = request.File.OpenReadStream
        };

        var result = await _profileService.UploadDocumentAsync(GetUserId(), request.DocumentType, fileRequest, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpPost("services")]
    public async Task<IActionResult> UpdateServices([FromBody] ServiceProviderServicesUpdateRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateServicesAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] ProviderCheckInRequest request, CancellationToken cancellationToken)
    {
        var result = await _providerAvailabilityService.CheckInAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpPost("check-out")]
    public async Task<IActionResult> CheckOut([FromBody] ProviderCheckOutRequest request, CancellationToken cancellationToken)
    {
        var result = await _providerAvailabilityService.CheckOutAsync(GetUserId(), request, cancellationToken);
        return result.ToActionResult();
    }

    [AuthorizeServiceProvider]
    [HttpGet("availability/today")]
    public async Task<IActionResult> GetTodayAvailabilityStatus(CancellationToken cancellationToken)
    {
        var result = await _providerAvailabilityService.GetTodayStatusAsync(GetUserId(), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
