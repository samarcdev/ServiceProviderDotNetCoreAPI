using APIServiceManagement.API.Attributes;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuthorizeMasterAdmin]
public class MasterAdminController : ControllerBase
{
    private readonly IMasterAdminService _masterAdminService;

    public MasterAdminController(IMasterAdminService masterAdminService)
    {
        _masterAdminService = masterAdminService;
    }

    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetDashboardStatsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllUsersAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("service-providers")]
    public async Task<IActionResult> GetAllServiceProviders(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllServiceProvidersAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("admins")]
    public async Task<IActionResult> GetAllAdmins(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllAdminsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("admins")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest request, CancellationToken cancellationToken)
    {
        var masterAdminId = GetUserId();
        var result = await _masterAdminService.CreateAdminAsync(request, masterAdminId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("admins/{adminId}/states")]
    public async Task<IActionResult> UpdateAdminStateAssignments(
        Guid adminId,
        [FromBody] UpdateAdminStateAssignmentsRequest request,
        CancellationToken cancellationToken)
    {
        var masterAdminId = GetUserId();
        var result = await _masterAdminService.UpdateAdminStateAssignmentsAsync(adminId, request, masterAdminId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("admins/{adminId}")]
    public async Task<IActionResult> DeleteAdmin(Guid adminId, CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.DeleteAdminAsync(adminId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetAllCategories(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllCategoriesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("categories")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var result = await _masterAdminService.CreateCategoryAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("categories/{categoryId}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UpdateCategory(
        int categoryId,
        [FromForm] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.UpdateCategoryAsync(categoryId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("categories/{categoryId}")]
    public async Task<IActionResult> DeleteCategory(int categoryId, CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.DeleteCategoryAsync(categoryId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetAllServices(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllServicesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("services")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CreateService([FromForm] CreateServiceRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var result = await _masterAdminService.CreateServiceAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("services/{serviceId}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UpdateService(
        int serviceId,
        [FromForm] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.UpdateServiceAsync(serviceId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("services/{serviceId}")]
    public async Task<IActionResult> DeleteService(int serviceId, CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.DeleteServiceAsync(serviceId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("states")]
    public async Task<IActionResult> GetAllStates(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllStatesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("states")]
    public async Task<IActionResult> CreateState([FromBody] CreateStateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var result = await _masterAdminService.CreateStateAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("states/{stateId}")]
    public async Task<IActionResult> UpdateState(
        int stateId,
        [FromBody] UpdateStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.UpdateStateAsync(stateId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("states/{stateId}")]
    public async Task<IActionResult> DeleteState(int stateId, CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.DeleteStateAsync(stateId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetAllCities(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllCitiesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("cities")]
    public async Task<IActionResult> CreateCity([FromBody] CreateCityRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var result = await _masterAdminService.CreateCityAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("cities/{cityId}")]
    public async Task<IActionResult> UpdateCity(
        int cityId,
        [FromBody] UpdateCityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.UpdateCityAsync(cityId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("cities/{cityId}")]
    public async Task<IActionResult> DeleteCity(int cityId, CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.DeleteCityAsync(cityId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("city-pincodes")]
    public async Task<IActionResult> GetAllCityPincodes(CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllCityPincodesAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("city-pincodes")]
    public async Task<IActionResult> CreateCityPincode([FromBody] CreateCityPincodeRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var result = await _masterAdminService.CreateCityPincodeAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("city-pincodes/{cityPincodeId}")]
    public async Task<IActionResult> UpdateCityPincode(
        int cityPincodeId,
        [FromBody] UpdateCityPincodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var result = await _masterAdminService.UpdateCityPincodeAsync(cityPincodeId, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("city-pincodes/{cityPincodeId}")]
    public async Task<IActionResult> DeleteCityPincode(int cityPincodeId, CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.DeleteCityPincodeAsync(cityPincodeId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("verifications")]
    public async Task<IActionResult> GetAllVerifications(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllVerificationsAsync(status, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings(
        [FromQuery] List<string>? statuses,
        CancellationToken cancellationToken)
    {
        var result = await _masterAdminService.GetAllBookingsAsync(statuses, cancellationToken);
        return result.ToActionResult();
    }

    private Guid? GetUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
