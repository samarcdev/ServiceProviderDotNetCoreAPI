using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IServiceProviderProfileService
{
    Task<ServiceResult> GetProfileAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetMissingDetailsAsync(Guid? userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateDetailsAsync(Guid? userId, ServiceProviderDetailsUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateProfileAsync(Guid? userId, ServiceProviderProfileUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UploadDocumentAsync(Guid? userId, string documentType, FileUploadRequest file, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateServicesAsync(Guid? userId, ServiceProviderServicesUpdateRequest request, CancellationToken cancellationToken = default);
}
