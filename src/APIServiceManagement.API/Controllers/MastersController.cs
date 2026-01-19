using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MastersController : ControllerBase
{
    private readonly IMasterDataService _masterDataService;

    public MastersController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }

    [AllowAnonymous]
    [HttpGet("states")]
    public async Task<IActionResult> GetStates(CancellationToken cancellationToken)
    {
        var states = await _masterDataService.GetStatesAsync(cancellationToken);
        return Ok(states);
    }

    [AllowAnonymous]
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] int? stateId, CancellationToken cancellationToken)
    {
        var cities = await _masterDataService.GetCitiesAsync(stateId, cancellationToken);
        return Ok(cities);
    }

    [AllowAnonymous]
    [HttpGet("cities/{cityId}/pincodes")]
    public async Task<IActionResult> GetCityPincodes(int cityId, CancellationToken cancellationToken)
    {
        var pincodes = await _masterDataService.GetCityPincodesAsync(cityId, cancellationToken);
        return Ok(pincodes);
    }

    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _masterDataService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [AllowAnonymous]
    [HttpGet("services")]
    public async Task<IActionResult> GetServices([FromQuery] int? categoryId, CancellationToken cancellationToken)
    {
        var services = await _masterDataService.GetServicesAsync(categoryId, cancellationToken);
        return Ok(services);
    }

    [AllowAnonymous]
    [HttpGet("services/by-pincode")]
    public async Task<IActionResult> GetServicesByPincode([FromQuery] string pincode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pincode))
        {
            return BadRequest(new { message = "Pincode is required." });
        }

        var services = await _masterDataService.GetServicesByPincodeAsync(pincode, cancellationToken);
        return Ok(services);
    }
}
