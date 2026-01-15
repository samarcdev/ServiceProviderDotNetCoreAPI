using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class BannerService : IBannerService
{
    private readonly AppDbContext _dbContext;

    public BannerService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<BannerResponse>> GetActiveBannersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Banners
            .AsNoTracking()
            .Where(banner =>
                banner.IsActive &&
                (banner.StartDate == null || banner.StartDate <= now) &&
                (banner.EndDate == null || banner.EndDate >= now))
            .OrderBy(banner => banner.DisplayOrder)
            .ThenBy(banner => banner.CreatedAt)
            .Select(banner => new BannerResponse
            {
                Id = banner.Id,
                Title = banner.Title,
                Subtitle = banner.Subtitle,
                Description = banner.Description,
                ImageUrl = banner.ImageUrl,
                BackgroundColor = banner.BackgroundColor,
                BackgroundGradient = banner.BackgroundGradient,
                ActionUrl = banner.ActionUrl,
                ActionText = banner.ActionText,
                DisplayOrder = banner.DisplayOrder,
                IsActive = banner.IsActive,
                DisplayDurationSeconds = banner.DisplayDurationSeconds,
                StartDate = banner.StartDate,
                EndDate = banner.EndDate,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
