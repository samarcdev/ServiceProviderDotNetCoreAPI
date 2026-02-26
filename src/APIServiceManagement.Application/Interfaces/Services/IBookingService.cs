using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IBookingService
{
    Task<ServiceResult> GetAllAvailableServicesAsync(Guid? customerId = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetTopBookedServicesAsync(int days = 90, int limit = 24, Guid? customerId = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAvailableServicesByPincodeAsync(string pincode, Guid? customerId = null, CancellationToken cancellationToken = default);
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
        string? search,
        CancellationToken cancellationToken = default);
    Task<ServiceResult> AssignServiceProviderAsync(Guid? adminId, BookingAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateBookingStatusAsync(Guid bookingId, BookingUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCustomerDashboardAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAdminDashboardAsync(Guid? adminId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetServiceProviderDashboardAsync(Guid? providerId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetUserPincodePreferencesAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveUserPincodePreferenceAsync(Guid? userId, PincodePreferenceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteUserPincodePreferenceAsync(Guid? userId, Guid preferenceId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetServiceTypesAsync(int? serviceId = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAvailableTimeSlotsAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetBookingSummaryAsync(Guid? userId, int serviceId, int? serviceTypeId, string pincode, DateTime? preferredDate, string? timeSlot, string? discountCode = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelBookingAsync(Guid? userId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAvailableServiceProvidersAsync(int serviceId, string pincode, DateTime? preferredDate = null, CancellationToken cancellationToken = default);
    Task<ServiceResult> AdminCancelBookingAsync(Guid? adminId, AdminCancelBookingRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> RequestRescheduleAsync(Guid? adminId, AdminRescheduleRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetRescheduleDetailsAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<ServiceResult> RespondToRescheduleAsync(Guid? customerId, CustomerRescheduleResponse request, CancellationToken cancellationToken = default);
}
