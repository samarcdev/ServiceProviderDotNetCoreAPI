using APIServiceManagement.API.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceProvidersController : ControllerBase
{
    private readonly IServiceProviderRegistrationService _registrationService;

    public ServiceProvidersController(IServiceProviderRegistrationService registrationService)
    {
        _registrationService = registrationService;
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
}
