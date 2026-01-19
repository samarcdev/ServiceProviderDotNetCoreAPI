using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Domain.Enums;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class MasterDataService : IMasterDataService
{
    private readonly AppDbContext _context;

    public MasterDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<State>> GetStatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.States
            .AsNoTracking()
            .Where(state => state.IsActive)
            .OrderBy(state => state.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<City>> GetCitiesAsync(int? stateId, CancellationToken cancellationToken = default)
    {
        var query = _context.Cities
            .AsNoTracking()
            .Where(city => city.IsActive);

        if (stateId.HasValue)
        {
            query = query.Where(city => city.StateId == stateId.Value);
        }

        return await query
            .OrderBy(city => city.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CityPincode>> GetCityPincodesAsync(int cityId, CancellationToken cancellationToken = default)
    {
        return await _context.CityPincodes
            .AsNoTracking()
            .Where(pincode => pincode.CityId == cityId && pincode.IsActive)
            .OrderBy(pincode => pincode.Pincode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.CategoryName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetServicesAsync(int? categoryId, CancellationToken cancellationToken = default)
    {
        var query = _context.Services
            .AsNoTracking()
            .Where(service => service.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(service => service.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(service => service.ServiceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetServicesByPincodeAsync(string pincode, CancellationToken cancellationToken = default)
    {
        var normalizedPincode = (pincode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPincode))
        {
            return new List<Service>();
        }

        var verifiedStatuses = new[] { VerificationStatusCodes.Verified, VerificationStatusCodes.Approved };

        var query =
            from providerService in _context.ProviderServices.AsNoTracking()
            join service in _context.Services.AsNoTracking()
                on providerService.ServiceId equals service.Id
            join address in _context.UsersAddresses.AsNoTracking()
                on providerService.UserId equals address.UserId
            join verification in _context.ServiceProviderVerifications.AsNoTracking()
                on providerService.UserId equals verification.ProviderUserId
            join user in _context.Users.AsNoTracking()
                on providerService.UserId equals user.Id
            join verificationStatus in _context.VerificationStatuses.AsNoTracking()
                on user.VerificationStatusId equals verificationStatus.Id
            where address.IsActive
                && address.ZipCode == normalizedPincode
                && verification.IsActive
                && verificationStatus.IsActive
                && verifiedStatuses.Contains(verificationStatus.Code.ToLower())
                && user.StatusId == (int)UserStatusEnum.Active
                && service.IsActive
            select service;

        return await query
            .Distinct()
            .OrderBy(service => service.ServiceName)
            .ToListAsync(cancellationToken);
    }
}
