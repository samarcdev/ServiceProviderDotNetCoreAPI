using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IBookingService
{
    Task<ServiceResult> GetAllAvailableServicesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAvailableServicesByPincodeAsync(string pincode, CancellationToken cancellationToken = default);
    Task<ServiceResult> ValidatePincodeAsync(string pincode, CancellationToken cancellationToken = default);
    Task<ServiceResult> CalculateServicePriceAsync(int serviceId, string pincode, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateBookingAsync(Guid? userId, BookingCreateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetBookingRequestsAsync(
        string? status,
        string? pincode,
        int? serviceId,
        Guid? customerId,
        Guid? serviceProviderId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sortBy,
        string? sortOrder,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> AssignServiceProviderAsync(Guid? adminId, BookingAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateBookingStatusAsync(Guid bookingId, BookingUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCustomerDashboardAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAdminDashboardAsync(Guid? adminId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetServiceProviderDashboardAsync(Guid? providerId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetUserPincodePreferencesAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveUserPincodePreferenceAsync(Guid? userId, PincodePreferenceRequest request, CancellationToken cancellationToken = default);
}
