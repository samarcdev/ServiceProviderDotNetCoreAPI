using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface ILeaveService
{
    Task<ServiceResult> ApplyLeaveAsync(Guid? serviceProviderId, ApplyLeaveRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetLeavesAsync(Guid? serviceProviderId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetBookingsForReassignmentAsync(Guid? serviceProviderId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<ServiceResult> IsServiceProviderOnLeaveAsync(Guid? serviceProviderId, DateTime date, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelLeaveDayAsync(Guid? serviceProviderId, DateTime date, CancellationToken cancellationToken = default);
}
