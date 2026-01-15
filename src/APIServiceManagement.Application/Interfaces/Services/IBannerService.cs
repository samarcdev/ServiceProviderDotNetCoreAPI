using APIServiceManagement.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IBannerService
{
    Task<IReadOnlyList<BannerResponse>> GetActiveBannersAsync(CancellationToken cancellationToken = default);
}
