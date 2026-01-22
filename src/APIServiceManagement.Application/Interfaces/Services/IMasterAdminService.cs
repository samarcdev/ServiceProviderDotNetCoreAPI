using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IMasterAdminService
{
    // Dashboard
    Task<ServiceResult> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    
    // User Management
    Task<ServiceResult> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> GetAllServiceProvidersAsync(CancellationToken cancellationToken = default);
    
    // Admin Management
    Task<ServiceResult> GetAllAdminsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateAdminAsync(CreateAdminRequest request, Guid? masterAdminId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateAdminStateAssignmentsAsync(Guid adminId, UpdateAdminStateAssignmentsRequest request, Guid? masterAdminId, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAdminAsync(Guid adminId, CancellationToken cancellationToken = default);
    
    // Master Data - Categories
    Task<ServiceResult> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    
    // Master Data - Services
    Task<ServiceResult> GetAllServicesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateServiceAsync(int serviceId, UpdateServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteServiceAsync(int serviceId, CancellationToken cancellationToken = default);
    
    // Master Data - States
    Task<ServiceResult> GetAllStatesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateStateAsync(CreateStateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateStateAsync(int stateId, UpdateStateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteStateAsync(int stateId, CancellationToken cancellationToken = default);
    
    // Master Data - Cities
    Task<ServiceResult> GetAllCitiesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateCityAsync(int cityId, UpdateCityRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteCityAsync(int cityId, CancellationToken cancellationToken = default);
    
    // Master Data - City Pincodes
    Task<ServiceResult> GetAllCityPincodesAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateCityPincodeAsync(CreateCityPincodeRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateCityPincodeAsync(int cityPincodeId, UpdateCityPincodeRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteCityPincodeAsync(int cityPincodeId, CancellationToken cancellationToken = default);
    
    // Verifications (all verifications for master admin)
    Task<ServiceResult> GetAllVerificationsAsync(string? status, CancellationToken cancellationToken = default);
    
    // Bookings (all bookings for master admin)
    Task<ServiceResult> GetAllBookingsAsync(List<string>? statuses, CancellationToken cancellationToken = default);
}
