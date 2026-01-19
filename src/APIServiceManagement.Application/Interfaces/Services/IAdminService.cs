using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IAdminService
{
    Task<ServiceResult> GetDashboardStatsAsync(Guid? adminId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetVerificationsByStatusAsync(Guid? adminId, string? status, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetVerificationDetailsAsync(int verificationId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateVerificationStatusAsync(Guid? adminId, int verificationId, UpdateVerificationStatusRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> CheckAdminPermissionsAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAdminAssignedStatesAsync(Guid? adminId, CancellationToken cancellationToken = default);
}
