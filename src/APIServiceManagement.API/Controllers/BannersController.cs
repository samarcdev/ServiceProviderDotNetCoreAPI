using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public BannersController(IBannerService bannerService)
    {
        _bannerService = bannerService;
    }

    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBanners(CancellationToken cancellationToken)
    {
        var banners = await _bannerService.GetActiveBannersAsync(cancellationToken);
        return Ok(banners);
    }
}
