using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IProviderAvailabilityService
{
    Task<ServiceResult> CheckInAsync(Guid? providerId, ProviderCheckInRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> CheckOutAsync(Guid? providerId, ProviderCheckOutRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetTodayStatusAsync(Guid? providerId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetActiveProvidersByPincodeAsync(string pincode, DateTime businessDateUtc, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetActiveProviderCountsByServiceIdsAndPincodeAsync(
        IReadOnlyCollection<int> serviceIds,
        string pincode,
        DateTime businessDateUtc,
        CancellationToken cancellationToken = default);
    Task<bool> IsAnyProviderAvailableForServiceAndPincodeAsync(
        int serviceId,
        string pincode,
        DateTime businessDateUtc,
        CancellationToken cancellationToken = default);
}
