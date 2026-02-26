using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Domain.Enums;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class ProviderAvailabilityService : IProviderAvailabilityService
{
    private readonly AppDbContext _context;

    public ProviderAvailabilityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> CheckInAsync(Guid? providerId, ProviderCheckInRequest request, CancellationToken cancellationToken = default)
    {
        if (!providerId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var today = DateTime.UtcNow.Date;

        var provider = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Id == providerId.Value &&
                u.RoleId == (int)RoleEnum.ServiceProvider &&
                u.StatusId == (int)UserStatusEnum.Active &&
                u.VerificationStatusId == (int)VerificationStatusEnum.Approved,
                cancellationToken);

        if (provider == null)
        {
            return ServiceResult.Forbidden("Only active and verified service providers can check in.");
        }

        var providerPincode = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.UserId == providerId.Value)
            .OrderByDescending(pref => pref.IsPrimary)
            .ThenBy(pref => pref.CreatedAt)
            .Select(pref => pref.Pincode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(providerPincode))
        {
            return ServiceResult.BadRequest(new ProviderAvailabilityStatusResponse
            {
                Success = false,
                Message = "No registered pincode found for this provider. Please configure pincode preferences first."
            });
        }

        var normalizedPincode = providerPincode.Trim();

        var isOnLeave = await _context.ServiceProviderLeaveDays
            .AsNoTracking()
            .AnyAsync(l => l.ServiceProviderId == providerId.Value && l.LeaveDate.Date == today, cancellationToken);

        if (isOnLeave)
        {
            return ServiceResult.BadRequest(new ProviderAvailabilityStatusResponse
            {
                Success = false,
                Message = "You cannot check in while on leave for today."
            });
        }

        var activeSession = await _context.ProviderAvailabilities
            .FirstOrDefaultAsync(pa =>
                pa.ServiceProviderId == providerId.Value &&
                pa.BusinessDate.Date == today &&
                pa.IsActive &&
                pa.CheckOutTimeUtc == null,
                cancellationToken);

        if (activeSession != null)
        {
            return ServiceResult.BadRequest(new ProviderAvailabilityStatusResponse
            {
                Success = false,
                Message = "You are already checked in for today.",
                IsCheckedIn = true,
                Pincode = activeSession.Pincode,
                BusinessDate = activeSession.BusinessDate,
                CheckInTimeUtc = activeSession.CheckInTimeUtc,
                CheckOutTimeUtc = activeSession.CheckOutTimeUtc
            });
        }

        var checkInTimeUtc = DateTime.UtcNow;
        var availability = new ProviderAvailability
        {
            ServiceProviderId = providerId.Value,
            Pincode = normalizedPincode,
            BusinessDate = today,
            CheckInTimeUtc = checkInTimeUtc,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            IsActive = true
        };

        _context.ProviderAvailabilities.Add(availability);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new ProviderAvailabilityStatusResponse
        {
            Success = true,
            Message = "Checked in successfully.",
            IsCheckedIn = true,
            Pincode = availability.Pincode,
            BusinessDate = availability.BusinessDate,
            CheckInTimeUtc = availability.CheckInTimeUtc,
            CheckOutTimeUtc = availability.CheckOutTimeUtc
        });
    }

    public async Task<ServiceResult> CheckOutAsync(Guid? providerId, ProviderCheckOutRequest request, CancellationToken cancellationToken = default)
    {
        if (!providerId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var today = DateTime.UtcNow.Date;
        var activeSession = await _context.ProviderAvailabilities
            .FirstOrDefaultAsync(pa =>
                pa.ServiceProviderId == providerId.Value &&
                pa.BusinessDate.Date == today &&
                pa.IsActive &&
                pa.CheckOutTimeUtc == null,
                cancellationToken);

        if (activeSession == null)
        {
            return ServiceResult.BadRequest(new ProviderAvailabilityStatusResponse
            {
                Success = false,
                Message = "No active check-in session found for today.",
                IsCheckedIn = false
            });
        }

        activeSession.CheckOutTimeUtc = DateTime.UtcNow;
        activeSession.CheckOutLatitude = request.Latitude;
        activeSession.CheckOutLongitude = request.Longitude;
        activeSession.IsActive = false;
        activeSession.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new ProviderAvailabilityStatusResponse
        {
            Success = true,
            Message = "Checked out successfully.",
            IsCheckedIn = false,
            Pincode = activeSession.Pincode,
            BusinessDate = activeSession.BusinessDate,
            CheckInTimeUtc = activeSession.CheckInTimeUtc,
            CheckOutTimeUtc = activeSession.CheckOutTimeUtc
        });
    }

    public async Task<ServiceResult> GetTodayStatusAsync(Guid? providerId, CancellationToken cancellationToken = default)
    {
        if (!providerId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var today = DateTime.UtcNow.Date;
        var latestSession = await _context.ProviderAvailabilities
            .AsNoTracking()
            .Where(pa => pa.ServiceProviderId == providerId.Value && pa.BusinessDate.Date == today)
            .OrderByDescending(pa => pa.CheckInTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSession == null)
        {
            return ServiceResult.Ok(new ProviderAvailabilityStatusResponse
            {
                Success = true,
                Message = "No check-in session found for today.",
                IsCheckedIn = false,
                BusinessDate = today
            });
        }

        return ServiceResult.Ok(new ProviderAvailabilityStatusResponse
        {
            Success = true,
            IsCheckedIn = latestSession.IsActive && latestSession.CheckOutTimeUtc == null,
            Pincode = latestSession.Pincode,
            BusinessDate = latestSession.BusinessDate,
            CheckInTimeUtc = latestSession.CheckInTimeUtc,
            CheckOutTimeUtc = latestSession.CheckOutTimeUtc
        });
    }

    public async Task<List<Guid>> GetActiveProvidersByPincodeAsync(string pincode, DateTime businessDateUtc, CancellationToken cancellationToken = default)
    {
        var normalizedPincode = pincode.Trim();
        var businessDate = businessDateUtc.Date;

        var pincodeProviders = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.Pincode == normalizedPincode)
            .Select(pref => pref.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (pincodeProviders.Count == 0)
        {
            return new List<Guid>();
        }

        var activeCheckedIn = await _context.ProviderAvailabilities
            .AsNoTracking()
            .Where(pa =>
                pincodeProviders.Contains(pa.ServiceProviderId) &&
                pa.BusinessDate.Date == businessDate &&
                pa.Pincode == normalizedPincode &&
                pa.IsActive &&
                pa.CheckOutTimeUtc == null)
            .Select(pa => pa.ServiceProviderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (activeCheckedIn.Count == 0)
        {
            return new List<Guid>();
        }

        var providersOnLeave = await _context.ServiceProviderLeaveDays
            .AsNoTracking()
            .Where(l => activeCheckedIn.Contains(l.ServiceProviderId) && l.LeaveDate.Date == businessDate)
            .Select(l => l.ServiceProviderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var providersOnLeaveSet = providersOnLeave.ToHashSet();
        var activeVerifiedProviders = await _context.Users
            .AsNoTracking()
            .Where(u =>
                activeCheckedIn.Contains(u.Id) &&
                u.RoleId == (int)RoleEnum.ServiceProvider &&
                u.StatusId == (int)UserStatusEnum.Active &&
                u.VerificationStatusId == (int)VerificationStatusEnum.Approved)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return activeVerifiedProviders.Where(id => !providersOnLeaveSet.Contains(id)).ToList();
    }

    public async Task<Dictionary<int, int>> GetActiveProviderCountsByServiceIdsAndPincodeAsync(
        IReadOnlyCollection<int> serviceIds,
        string pincode,
        DateTime businessDateUtc,
        CancellationToken cancellationToken = default)
    {
        if (serviceIds == null || serviceIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        var activeProviderIds = await GetActiveProvidersByPincodeAsync(pincode, businessDateUtc, cancellationToken);
        if (activeProviderIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await _context.ProviderServices
            .AsNoTracking()
            .Where(ps => ps.IsActive && serviceIds.Contains(ps.ServiceId) && activeProviderIds.Contains(ps.UserId))
            .GroupBy(ps => ps.ServiceId)
            .Select(group => new { ServiceId = group.Key, Count = group.Select(ps => ps.UserId).Distinct().Count() })
            .ToDictionaryAsync(item => item.ServiceId, item => item.Count, cancellationToken);
    }

    public async Task<bool> IsAnyProviderAvailableForServiceAndPincodeAsync(
        int serviceId,
        string pincode,
        DateTime businessDateUtc,
        CancellationToken cancellationToken = default)
    {
        var activeProviderIds = await GetActiveProvidersByPincodeAsync(pincode, businessDateUtc, cancellationToken);
        if (activeProviderIds.Count == 0)
        {
            return false;
        }

        return await _context.ProviderServices
            .AsNoTracking()
            .AnyAsync(ps => ps.IsActive && ps.ServiceId == serviceId && activeProviderIds.Contains(ps.UserId), cancellationToken);
    }
}
