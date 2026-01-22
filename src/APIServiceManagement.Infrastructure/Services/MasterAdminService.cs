using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Domain.Enums;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class MasterAdminService : IMasterAdminService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IFileStorageService _fileStorageService;

    public MasterAdminService(AppDbContext context, IPasswordHasher passwordHasher, IFileStorageService fileStorageService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _fileStorageService = fileStorageService;
    }

    public async Task<ServiceResult> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = new MasterAdminDashboardStatsResponse();

            // Total users
            stats.TotalUsers = await _context.Users.CountAsync(cancellationToken);

            // Total admins (excluding MasterAdmin)
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Admin && r.IsActive)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (adminRoleId.HasValue)
            {
                stats.TotalAdmins = await _context.Users
                    .CountAsync(u => u.RoleId == adminRoleId.Value && u.StatusId == (int)UserStatusEnum.Active, cancellationToken);
            }

            // Total service providers
            var serviceProviderRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.ServiceProvider && r.IsActive)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (serviceProviderRoleId.HasValue)
            {
                stats.TotalServiceProviders = await _context.Users
                    .CountAsync(u => u.RoleId == serviceProviderRoleId.Value && u.StatusId == (int)UserStatusEnum.Active, cancellationToken);
            }

            // Total customers
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Customer && r.IsActive)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (customerRoleId.HasValue)
            {
                stats.TotalCustomers = await _context.Users
                    .CountAsync(u => u.RoleId == customerRoleId.Value && u.StatusId == (int)UserStatusEnum.Active, cancellationToken);
            }

            // Pending verifications
            var pendingStatusId = (int)VerificationStatusEnum.Pending;
            stats.PendingVerifications = await _context.Users
                .CountAsync(u => u.VerificationStatusId == pendingStatusId && u.StatusId == (int)UserStatusEnum.Active, cancellationToken);

            // Bookings stats
            stats.TotalBookings = await _context.BookingRequests.CountAsync(cancellationToken);
            
            var pendingBookingStatusId = await _context.BookingStatuses
                .Where(s => s.Code == BookingStatusCodes.Pending && s.IsActive)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (pendingBookingStatusId.HasValue)
            {
                stats.PendingBookings = await _context.BookingRequests
                    .CountAsync(b => b.StatusId == pendingBookingStatusId.Value, cancellationToken);
            }

            var completedBookingStatusId = await _context.BookingStatuses
                .Where(s => s.Code == BookingStatusCodes.Completed && s.IsActive)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
            
            if (completedBookingStatusId.HasValue)
            {
                stats.CompletedBookings = await _context.BookingRequests
                    .CountAsync(b => b.StatusId == completedBookingStatusId.Value, cancellationToken);
            }

            // Master data counts
            stats.TotalCategories = await _context.Categories.CountAsync(cancellationToken);
            stats.TotalServices = await _context.Services.CountAsync(cancellationToken);
            stats.TotalStates = await _context.States.CountAsync(cancellationToken);
            stats.TotalCities = await _context.Cities.CountAsync(cancellationToken);

            return ServiceResult.Ok(stats);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching dashboard stats: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get role ID for Customer only
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Customer && r.IsActive)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!customerRoleId.HasValue)
            {
                return ServiceResult.Ok(new List<UserManagementResponse>());
            }

            // Filter users to only include Customer role
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.VerificationStatus)
                .Where(u => u.RoleId == customerRoleId.Value)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToList();
            var usersExtraInfo = await _context.UsersExtraInfos
                .AsNoTracking()
                .Where(uei => uei.UserId.HasValue && userIds.Contains(uei.UserId.Value))
                .ToListAsync(cancellationToken);

            var response = users.Select(u =>
            {
                var extraInfo = usersExtraInfo.FirstOrDefault(uei => uei.UserId == u.Id);
                var verificationStatus = u.VerificationStatus?.Code?.ToLowerInvariant();
                var isVerified = verificationStatus == VerificationStatusCodes.Approved.ToLowerInvariant() 
                    || verificationStatus == VerificationStatusCodes.Verified.ToLowerInvariant();

                return new UserManagementResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = extraInfo?.FullName ?? u.Name,
                    PhoneNumber = extraInfo?.PhoneNumber ?? u.MobileNumber,
                    UserType = u.Role?.Name?.ToLowerInvariant() ?? "unknown",
                    RoleName = u.Role?.Name ?? "Unknown",
                    IsVerified = isVerified,
                    VerificationStatus = verificationStatus,
                    CreatedAt = u.CreatedAt
                };
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching users: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetAllServiceProvidersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get role ID for ServiceProvider only
            var serviceProviderRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.ServiceProvider && r.IsActive)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!serviceProviderRoleId.HasValue)
            {
                return ServiceResult.Ok(new List<UserManagementResponse>());
            }

            // Filter users to only include ServiceProvider role
            var users = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.VerificationStatus)
                .Where(u => u.RoleId == serviceProviderRoleId.Value)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            var userIds = users.Select(u => u.Id).ToList();
            var usersExtraInfo = await _context.UsersExtraInfos
                .AsNoTracking()
                .Where(uei => uei.UserId.HasValue && userIds.Contains(uei.UserId.Value))
                .ToListAsync(cancellationToken);

            var response = users.Select(u =>
            {
                var extraInfo = usersExtraInfo.FirstOrDefault(uei => uei.UserId == u.Id);
                var verificationStatus = u.VerificationStatus?.Code?.ToLowerInvariant();
                var isVerified = verificationStatus == VerificationStatusCodes.Approved.ToLowerInvariant() 
                    || verificationStatus == VerificationStatusCodes.Verified.ToLowerInvariant();

                return new UserManagementResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = extraInfo?.FullName ?? u.Name,
                    PhoneNumber = extraInfo?.PhoneNumber ?? u.MobileNumber,
                    UserType = u.Role?.Name?.ToLowerInvariant() ?? "unknown",
                    RoleName = u.Role?.Name ?? "Unknown",
                    IsVerified = isVerified,
                    VerificationStatus = verificationStatus,
                    CreatedAt = u.CreatedAt
                };
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching service providers: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetAllAdminsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var adminRoleId = await _context.Roles
                .Where(r => r.Name == RoleNames.Admin && r.IsActive)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!adminRoleId.HasValue)
            {
                return ServiceResult.Ok(new List<AdminManagementResponse>());
            }

            var admins = await _context.Users
                .AsNoTracking()
                .Where(u => u.RoleId == adminRoleId.Value && u.StatusId == (int)UserStatusEnum.Active)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            var adminIds = admins.Select(a => a.Id).ToList();
            var usersExtraInfo = await _context.UsersExtraInfos
                .AsNoTracking()
                .Where(uei => uei.UserId.HasValue && adminIds.Contains(uei.UserId.Value))
                .ToListAsync(cancellationToken);

            var assignments = await _context.AdminStateAssignments
                .AsNoTracking()
                .Where(a => adminIds.Contains(a.AdminUserId))
                .Include(a => a.State)
                .ToListAsync(cancellationToken);

            var response = admins.Select(admin =>
            {
                var extraInfo = usersExtraInfo.FirstOrDefault(uei => uei.UserId == admin.Id);
                var adminAssignments = assignments.Where(a => a.AdminUserId == admin.Id).ToList();

                return new AdminManagementResponse
                {
                    Id = admin.Id,
                    Email = admin.Email,
                    FullName = extraInfo?.FullName ?? admin.Name,
                    PhoneNumber = extraInfo?.PhoneNumber ?? admin.MobileNumber,
                    AssignedStates = adminAssignments.Select(a => new AdminAssignedStateResponse
                    {
                        Id = a.Id,
                        StateId = a.StateId,
                        StateName = a.State?.Name ?? "Unknown"
                    }).ToList(),
                    CreatedAt = admin.CreatedAt
                };
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching admins: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CreateAdminAsync(CreateAdminRequest request, Guid? masterAdminId, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            // Check if email already exists
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

            if (existingUser)
            {
                return ServiceResult.BadRequest("Email is already registered.");
            }

            // Get admin role
            var adminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == RoleNames.Admin && r.IsActive, cancellationToken);

            if (adminRole == null)
            {
                return ServiceResult.BadRequest("Admin role not found.");
            }

            // Create user
            var salt = _passwordHasher.GenerateSalt();
            var hash = _passwordHasher.HashPassword(request.Password, salt);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.FullName.Trim(),
                Email = normalizedEmail,
                MobileNumber = request.PhoneNumber.Trim(),
                PasswordSalt = salt,
                PasswordHash = hash,
                PasswordSlug = Guid.NewGuid().ToString("N"),
                RoleId = adminRole.Id,
                StatusId = (int)UserStatusEnum.Active,
                VerificationStatusId = (int)VerificationStatusEnum.Approved
            };

            _context.Users.Add(user);

            // Create users extra info
            var extraInfo = new UsersExtraInfo
            {
                UserId = user.Id,
                FullName = request.FullName.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                Email = normalizedEmail,
                IsMobileVerified = true,
                IsAcceptedTerms = true,
                IsCompleted = true
            };

            _context.UsersExtraInfos.Add(extraInfo);

            // Assign state - only one state per admin
            var alreadyAssignedStates = new List<AlreadyAssignedStateInfo>();
            int? assignedStateId = null;
            string? assignedStateName = null;

            if (request.StateIds != null && request.StateIds.Count > 0)
            {
                // Take only the first state (one admin can only have one state)
                var requestedStateId = request.StateIds.First();

                // Check if state exists and is active
                var state = await _context.States
                    .FirstOrDefaultAsync(s => s.Id == requestedStateId && s.IsActive, cancellationToken);

                if (state != null)
                {
                    // Check if this state is already assigned to another admin
                    var existingAssignment = await _context.AdminStateAssignments
                        .Include(a => a.AdminUser)
                        .Include(a => a.State)
                        .FirstOrDefaultAsync(a => a.StateId == requestedStateId, cancellationToken);

                    if (existingAssignment != null)
                    {
                        // State is already assigned to another admin
                        alreadyAssignedStates.Add(new AlreadyAssignedStateInfo
                        {
                            StateId = state.Id,
                            StateName = state.Name,
                            AssignedToAdminId = existingAssignment.AdminUserId,
                            AssignedToAdminEmail = existingAssignment.AdminUser?.Email ?? "Unknown"
                        });
                    }
                    else
                    {
                        // State is available, assign it
                        var assignment = new AdminStateAssignment
                        {
                            AdminUserId = user.Id,
                            StateId = requestedStateId,
                            AssignedByUserId = masterAdminId,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.AdminStateAssignments.Add(assignment);
                        assignedStateId = state.Id;
                        assignedStateName = state.Name;
                    }
                }

                // Check other requested states (if multiple were provided) and add them to already assigned list
                if (request.StateIds.Count > 1)
                {
                    var otherStateIds = request.StateIds.Skip(1).ToList();
                    var otherStates = await _context.States
                        .Where(s => otherStateIds.Contains(s.Id) && s.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var otherState in otherStates)
                    {
                        var existingAssignment = await _context.AdminStateAssignments
                            .Include(a => a.AdminUser)
                            .FirstOrDefaultAsync(a => a.StateId == otherState.Id, cancellationToken);

                        if (existingAssignment != null)
                        {
                            alreadyAssignedStates.Add(new AlreadyAssignedStateInfo
                            {
                                StateId = otherState.Id,
                                StateName = otherState.Name,
                                AssignedToAdminId = existingAssignment.AdminUserId,
                                AssignedToAdminEmail = existingAssignment.AdminUser?.Email ?? "Unknown"
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var response = new CreateAdminResponse
            {
                Success = true,
                Message = alreadyAssignedStates.Count > 0
                    ? $"Admin created successfully. {(assignedStateId.HasValue ? $"State '{assignedStateName}' assigned. " : "")}Some requested states are already assigned to other admins."
                    : assignedStateId.HasValue
                        ? $"Admin created successfully. State '{assignedStateName}' assigned."
                        : "Admin created successfully. No state assigned.",
                AdminId = user.Id,
                AlreadyAssignedStates = alreadyAssignedStates,
                AssignedStateId = assignedStateId,
                AssignedStateName = assignedStateName
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error creating admin: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateAdminStateAssignmentsAsync(
        Guid adminId, 
        UpdateAdminStateAssignmentsRequest request, 
        Guid? masterAdminId, 
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            // Verify admin exists
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == adminId, cancellationToken);

            if (admin == null)
            {
                return ServiceResult.NotFound("Admin not found.");
            }

            // Remove existing assignments
            var existingAssignments = await _context.AdminStateAssignments
                .Where(a => a.AdminUserId == adminId)
                .ToListAsync(cancellationToken);

            _context.AdminStateAssignments.RemoveRange(existingAssignments);

            // Add new assignment - only one state per admin
            var alreadyAssignedStates = new List<AlreadyAssignedStateInfo>();
            int? assignedStateId = null;
            string? assignedStateName = null;

            if (request.StateIds != null && request.StateIds.Count > 0)
            {
                // Take only the first state (one admin can only have one state)
                var requestedStateId = request.StateIds.First();

                // Check if state exists and is active
                var state = await _context.States
                    .FirstOrDefaultAsync(s => s.Id == requestedStateId && s.IsActive, cancellationToken);

                if (state != null)
                {
                    // Check if this state is already assigned to another admin (not the current admin)
                    var existingAssignment = await _context.AdminStateAssignments
                        .Include(a => a.AdminUser)
                        .Include(a => a.State)
                        .FirstOrDefaultAsync(a => a.StateId == requestedStateId && a.AdminUserId != adminId, cancellationToken);

                    if (existingAssignment != null)
                    {
                        // State is already assigned to another admin
                        alreadyAssignedStates.Add(new AlreadyAssignedStateInfo
                        {
                            StateId = state.Id,
                            StateName = state.Name,
                            AssignedToAdminId = existingAssignment.AdminUserId,
                            AssignedToAdminEmail = existingAssignment.AdminUser?.Email ?? "Unknown"
                        });
                    }
                    else
                    {
                        // State is available or already assigned to this admin, assign/reassign it
                        var assignment = new AdminStateAssignment
                        {
                            AdminUserId = adminId,
                            StateId = requestedStateId,
                            AssignedByUserId = masterAdminId,
                            AssignedAt = DateTime.UtcNow
                        };
                        _context.AdminStateAssignments.Add(assignment);
                        assignedStateId = state.Id;
                        assignedStateName = state.Name;
                    }
                }

                // Check other requested states (if multiple were provided) and add them to already assigned list
                if (request.StateIds.Count > 1)
                {
                    var otherStateIds = request.StateIds.Skip(1).ToList();
                    var otherStates = await _context.States
                        .Where(s => otherStateIds.Contains(s.Id) && s.IsActive)
                        .ToListAsync(cancellationToken);

                    foreach (var otherState in otherStates)
                    {
                        var existingAssignment = await _context.AdminStateAssignments
                            .Include(a => a.AdminUser)
                            .FirstOrDefaultAsync(a => a.StateId == otherState.Id, cancellationToken);

                        if (existingAssignment != null)
                        {
                            alreadyAssignedStates.Add(new AlreadyAssignedStateInfo
                            {
                                StateId = otherState.Id,
                                StateName = otherState.Name,
                                AssignedToAdminId = existingAssignment.AdminUserId,
                                AssignedToAdminEmail = existingAssignment.AdminUser?.Email ?? "Unknown"
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var response = new CreateAdminResponse
            {
                Success = true,
                Message = alreadyAssignedStates.Count > 0
                    ? $"Admin state assignments updated. {(assignedStateId.HasValue ? $"State '{assignedStateName}' assigned. " : "")}Some requested states are already assigned to other admins."
                    : assignedStateId.HasValue
                        ? $"Admin state assignments updated. State '{assignedStateName}' assigned."
                        : "Admin state assignments updated. No state assigned.",
                AdminId = adminId,
                AlreadyAssignedStates = alreadyAssignedStates,
                AssignedStateId = assignedStateId,
                AssignedStateName = assignedStateName
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating admin state assignments: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteAdminAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        try
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == adminId, cancellationToken);

            if (admin == null)
            {
                return ServiceResult.NotFound("Admin not found.");
            }

            // Deactivate admin instead of deleting
            admin.StatusId = (int)UserStatusEnum.Inactive;

            // Remove state assignments
            var assignments = await _context.AdminStateAssignments
                .Where(a => a.AdminUserId == adminId)
                .ToListAsync(cancellationToken);

            _context.AdminStateAssignments.RemoveRange(assignments);

            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "Admin deactivated successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error deleting admin: {ex.Message}");
        }
    }

    // Master Data - Categories
    public async Task<ServiceResult> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .ToListAsync(cancellationToken);

            var response = categories.Select(c => new MasterDataCategoryResponse
            {
                Id = c.Id,
                CategoryName = c.CategoryName,
                ParentId = c.ParentId,
                IsActive = c.IsActive,
                Image = c.Image,
                Icon = c.Icon,
                CreatedAt = c.CreatedAt
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching categories: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return ServiceResult.BadRequest("Category name is required.");
        }

        try
        {
            // Check for duplicate category name
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == request.CategoryName.Trim().ToLower(), cancellationToken);

            if (existingCategory != null)
            {
                return ServiceResult.BadRequest("A category with this name already exists.");
            }

            // Validate parent category if provided
            if (request.ParentId.HasValue)
            {
                var parentCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.ParentId.Value && c.IsActive, cancellationToken);

                if (parentCategory == null)
                {
                    return ServiceResult.BadRequest("Parent category not found or inactive.");
                }
            }

            // Handle file uploads
            var imagePath = await SaveFileAsync(request.Image, "categories/images", cancellationToken);
            var iconPath = await SaveFileAsync(request.Icon, "categories/icons", cancellationToken);

            var category = new Category
            {
                CategoryName = request.CategoryName.Trim(),
                ParentId = request.ParentId,
                Image = imagePath,
                Icon = iconPath,
                IsActive = true
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new MasterDataCategoryResponse
            {
                Id = category.Id,
                CategoryName = category.CategoryName,
                ParentId = category.ParentId,
                IsActive = category.IsActive,
                Image = category.Image,
                Icon = category.Icon,
                CreatedAt = category.CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error creating category: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category == null)
            {
                return ServiceResult.NotFound("Category not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                category.CategoryName = request.CategoryName.Trim();
            }
            if (request.ParentId.HasValue)
            {
                category.ParentId = request.ParentId.Value;
            }
            
            // Handle image upload/update
            if (request.Image != null && request.Image.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrWhiteSpace(category.Image))
                {
                    await _fileStorageService.DeleteAsync(category.Image, cancellationToken);
                }

                category.Image = await SaveFileAsync(request.Image, "categories/images", cancellationToken);
            }
            
            // Handle icon upload/update
            if (request.Icon != null && request.Icon.Length > 0)
            {
                // Delete old icon if exists
                if (!string.IsNullOrWhiteSpace(category.Icon))
                {
                    await _fileStorageService.DeleteAsync(category.Icon, cancellationToken);
                }

                category.Icon = await SaveFileAsync(request.Icon, "categories/icons", cancellationToken);
            }
            
            if (request.IsActive.HasValue)
            {
                category.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "Category updated successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating category: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

            if (category == null)
            {
                return ServiceResult.NotFound("Category not found.");
            }

            // Deactivate instead of delete
            category.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "Category deleted successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error deleting category: {ex.Message}");
        }
    }

    // Master Data - Services
    public async Task<ServiceResult> GetAllServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await _context.Services
                .AsNoTracking()
                .Include(s => s.Category)
                .OrderBy(s => s.ServiceName)
                .ToListAsync(cancellationToken);

            var response = services.Select(s => new MasterDataServiceResponse
            {
                Id = s.Id,
                ServiceName = s.ServiceName,
                Description = s.Description,
                CategoryId = s.CategoryId ?? 0,
                CategoryName = s.Category?.CategoryName ?? "Unknown",
                IsActive = s.IsActive,
                Image = s.Image,
                Icon = s.Icon,
                CreatedAt = s.CreatedAt
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching services: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ServiceName))
        {
            return ServiceResult.BadRequest("Service name is required.");
        }

        if (request.CategoryId <= 0)
        {
            return ServiceResult.BadRequest("Valid category ID is required.");
        }

        try
        {
            // Verify category exists
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);

            if (category == null)
            {
                return ServiceResult.BadRequest("Category not found or inactive.");
            }

            // Check for duplicate service name in the same category
            var existingService = await _context.Services
                .FirstOrDefaultAsync(s => s.ServiceName.ToLower() == request.ServiceName.Trim().ToLower() 
                    && s.CategoryId == request.CategoryId, cancellationToken);

            if (existingService != null)
            {
                return ServiceResult.BadRequest("A service with this name already exists in this category.");
            }

            // Handle file uploads
            var imagePath = await SaveFileAsync(request.Image, "services/images", cancellationToken);
            var iconPath = await SaveFileAsync(request.Icon, "services/icons", cancellationToken);

            var service = new Service
            {
                ServiceName = request.ServiceName.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? "" : request.Description.Trim(),
                CategoryId = request.CategoryId,
                Image = imagePath,
                Icon = iconPath,
                IsActive = true
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new MasterDataServiceResponse
            {
                Id = service.Id,
                ServiceName = service.ServiceName,
                Description = service.Description,
                CategoryId = service.CategoryId ?? 0,
                CategoryName = category.CategoryName,
                IsActive = service.IsActive,
                Image = service.Image,
                Icon = service.Icon,
                CreatedAt = service.CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error creating service: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateServiceAsync(int serviceId, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

            if (service == null)
            {
                return ServiceResult.NotFound("Service not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.ServiceName))
            {
                service.ServiceName = request.ServiceName.Trim();
            }
            if (request.Description != null)
            {
                service.Description = request.Description;
            }
            if (request.CategoryId.HasValue)
            {
                service.CategoryId = request.CategoryId.Value;
            }
            
            // Handle image upload/update
            if (request.Image != null && request.Image.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrWhiteSpace(service.Image))
                {
                    await _fileStorageService.DeleteAsync(service.Image, cancellationToken);
                }

                service.Image = await SaveFileAsync(request.Image, "services/images", cancellationToken);
            }
            
            // Handle icon upload/update
            if (request.Icon != null && request.Icon.Length > 0)
            {
                // Delete old icon if exists
                if (!string.IsNullOrWhiteSpace(service.Icon))
                {
                    await _fileStorageService.DeleteAsync(service.Icon, cancellationToken);
                }

                service.Icon = await SaveFileAsync(request.Icon, "services/icons", cancellationToken);
            }
            
            if (request.IsActive.HasValue)
            {
                service.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "Service updated successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating service: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteServiceAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);

            if (service == null)
            {
                return ServiceResult.NotFound("Service not found.");
            }

            // Deactivate instead of delete
            service.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "Service deleted successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error deleting service: {ex.Message}");
        }
    }

    // Master Data - States
    public async Task<ServiceResult> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var states = await _context.States
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            var response = states.Select(s => new MasterDataStateResponse
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching states: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CreateStateAsync(CreateStateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult.BadRequest("State name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ServiceResult.BadRequest("State code is required.");
        }

        try
        {
            var trimmedName = request.Name.Trim();
            var trimmedCode = request.Code.Trim().ToUpperInvariant();

            // Check for duplicate state name
            var existingStateByName = await _context.States
                .FirstOrDefaultAsync(s => s.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

            if (existingStateByName != null)
            {
                return ServiceResult.BadRequest("A state with this name already exists.");
            }

            // Check for duplicate state code
            var existingStateByCode = await _context.States
                .FirstOrDefaultAsync(s => s.Code.ToUpper() == trimmedCode, cancellationToken);

            if (existingStateByCode != null)
            {
                return ServiceResult.BadRequest("A state with this code already exists.");
            }

            var state = new State
            {
                Name = trimmedName,
                Code = trimmedCode,
                IsActive = true
            };

            _context.States.Add(state);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new MasterDataStateResponse
            {
                Id = state.Id,
                Name = state.Name,
                Code = state.Code,
                IsActive = state.IsActive,
                CreatedAt = state.CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error creating state: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateStateAsync(int stateId, UpdateStateRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            var state = await _context.States
                .FirstOrDefaultAsync(s => s.Id == stateId, cancellationToken);

            if (state == null)
            {
                return ServiceResult.NotFound("State not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                state.Name = request.Name.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                state.Code = request.Code.Trim().ToUpperInvariant();
            }
            if (request.IsActive.HasValue)
            {
                state.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "State updated successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating state: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteStateAsync(int stateId, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await _context.States
                .FirstOrDefaultAsync(s => s.Id == stateId, cancellationToken);

            if (state == null)
            {
                return ServiceResult.NotFound("State not found.");
            }

            // Deactivate instead of delete
            state.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "State deleted successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error deleting state: {ex.Message}");
        }
    }

    // Master Data - Cities
    public async Task<ServiceResult> GetAllCitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cities = await _context.Cities
                .AsNoTracking()
                .Include(c => c.State)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var response = cities.Select(c => new MasterDataCityResponse
            {
                Id = c.Id,
                Name = c.Name,
                StateId = c.StateId ?? 0,
                StateName = c.State?.Name ?? "Unknown",
                Pincode = null, // Cities don't have direct pincode, it's in CityPincode table
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching cities: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CreateCityAsync(CreateCityRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult.BadRequest("City name is required.");
        }

        if (request.StateId <= 0)
        {
            return ServiceResult.BadRequest("Valid state ID is required.");
        }

        try
        {
            // Verify state exists
            var state = await _context.States
                .FirstOrDefaultAsync(s => s.Id == request.StateId && s.IsActive, cancellationToken);

            if (state == null)
            {
                return ServiceResult.BadRequest("State not found or inactive.");
            }

            var trimmedName = request.Name.Trim();

            // Check for duplicate city name in the same state
            var existingCity = await _context.Cities
                .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower() 
                    && c.StateId == request.StateId, cancellationToken);

            if (existingCity != null)
            {
                return ServiceResult.BadRequest("A city with this name already exists in this state.");
            }

            var city = new City
            {
                Name = trimmedName,
                StateId = request.StateId,
                IsActive = true
            };

            _context.Cities.Add(city);
            await _context.SaveChangesAsync(cancellationToken);

            // If pincode is provided, create a CityPincode entry
            if (!string.IsNullOrWhiteSpace(request.Pincode))
            {
                var trimmedPincode = request.Pincode.Trim();
                
                // Check if pincode already exists for this city
                var existingPincode = await _context.CityPincodes
                    .FirstOrDefaultAsync(cp => cp.CityId == city.Id && cp.Pincode == trimmedPincode, cancellationToken);

                if (existingPincode == null)
                {
                    var cityPincode = new CityPincode
                    {
                        CityId = city.Id,
                        Pincode = trimmedPincode,
                        IsActive = true
                    };

                    _context.CityPincodes.Add(cityPincode);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            var response = new MasterDataCityResponse
            {
                Id = city.Id,
                Name = city.Name,
                StateId = city.StateId ?? 0,
                StateName = state.Name,
                Pincode = request.Pincode,
                IsActive = city.IsActive,
                CreatedAt = city.CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error creating city: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateCityAsync(int cityId, UpdateCityRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

            if (city == null)
            {
                return ServiceResult.NotFound("City not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                city.Name = request.Name.Trim();
            }
            if (request.StateId.HasValue)
            {
                city.StateId = request.StateId.Value;
            }
            if (request.IsActive.HasValue)
            {
                city.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "City updated successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating city: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteCityAsync(int cityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

            if (city == null)
            {
                return ServiceResult.NotFound("City not found.");
            }

            // Deactivate instead of delete
            city.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "City deleted successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error deleting city: {ex.Message}");
        }
    }

    // Master Data - City Pincodes
    public async Task<ServiceResult> GetAllCityPincodesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cityPincodes = await _context.CityPincodes
                .AsNoTracking()
                .Include(cp => cp.City)
                    .ThenInclude(c => c.State)
                .OrderBy(cp => cp.City != null ? cp.City.Name : "")
                .ThenBy(cp => cp.Pincode)
                .ToListAsync(cancellationToken);

            var response = cityPincodes.Select(cp => new MasterDataCityPincodeResponse
            {
                Id = cp.Id,
                CityId = cp.CityId,
                CityName = cp.City?.Name ?? "Unknown",
                StateId = cp.City?.StateId ?? 0,
                StateName = cp.City?.State?.Name ?? "Unknown",
                Pincode = cp.Pincode,
                IsActive = cp.IsActive,
                CreatedAt = cp.CreatedAt
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching city pincodes: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CreateCityPincodeAsync(CreateCityPincodeRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        if (request.CityId <= 0)
        {
            return ServiceResult.BadRequest("Valid city ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Pincode))
        {
            return ServiceResult.BadRequest("Pincode is required.");
        }

        try
        {
            // Verify city exists
            var city = await _context.Cities
                .Include(c => c.State)
                .FirstOrDefaultAsync(c => c.Id == request.CityId && c.IsActive, cancellationToken);

            if (city == null)
            {
                return ServiceResult.BadRequest("City not found or inactive.");
            }

            var trimmedPincode = request.Pincode.Trim();

            // Validate pincode format (6 digits)
            if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedPincode, @"^\d{6}$"))
            {
                return ServiceResult.BadRequest("Pincode must be exactly 6 digits.");
            }

            // Check for duplicate pincode in the same city
            var existingPincode = await _context.CityPincodes
                .FirstOrDefaultAsync(cp => cp.CityId == request.CityId && cp.Pincode == trimmedPincode, cancellationToken);

            if (existingPincode != null)
            {
                return ServiceResult.BadRequest("This pincode already exists for this city.");
            }

            var cityPincode = new CityPincode
            {
                CityId = request.CityId,
                Pincode = trimmedPincode,
                IsActive = true
            };

            _context.CityPincodes.Add(cityPincode);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new MasterDataCityPincodeResponse
            {
                Id = cityPincode.Id,
                CityId = cityPincode.CityId,
                CityName = city.Name,
                StateId = city.StateId ?? 0,
                StateName = city.State?.Name ?? "Unknown",
                Pincode = cityPincode.Pincode,
                IsActive = cityPincode.IsActive,
                CreatedAt = cityPincode.CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error creating city pincode: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateCityPincodeAsync(int cityPincodeId, UpdateCityPincodeRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return ServiceResult.BadRequest("Request is required.");
        }

        try
        {
            var cityPincode = await _context.CityPincodes
                .Include(cp => cp.City)
                    .ThenInclude(c => c.State)
                .FirstOrDefaultAsync(cp => cp.Id == cityPincodeId, cancellationToken);

            if (cityPincode == null)
            {
                return ServiceResult.NotFound("City pincode not found.");
            }

            // Update city if provided
            if (request.CityId.HasValue && request.CityId.Value != cityPincode.CityId)
            {
                var newCity = await _context.Cities
                    .Include(c => c.State)
                    .FirstOrDefaultAsync(c => c.Id == request.CityId.Value && c.IsActive, cancellationToken);

                if (newCity == null)
                {
                    return ServiceResult.BadRequest("City not found or inactive.");
                }

                cityPincode.CityId = request.CityId.Value;
            }

            // Update pincode if provided
            if (!string.IsNullOrWhiteSpace(request.Pincode))
            {
                var trimmedPincode = request.Pincode.Trim();

                // Validate pincode format
                if (!System.Text.RegularExpressions.Regex.IsMatch(trimmedPincode, @"^\d{6}$"))
                {
                    return ServiceResult.BadRequest("Pincode must be exactly 6 digits.");
                }

                // Check for duplicate pincode in the same city
                var existingPincode = await _context.CityPincodes
                    .FirstOrDefaultAsync(cp => cp.CityId == cityPincode.CityId 
                        && cp.Pincode == trimmedPincode 
                        && cp.Id != cityPincodeId, cancellationToken);

                if (existingPincode != null)
                {
                    return ServiceResult.BadRequest("This pincode already exists for this city.");
                }

                cityPincode.Pincode = trimmedPincode;
            }

            // Update active status if provided
            if (request.IsActive.HasValue)
            {
                cityPincode.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Reload city data for response
            await _context.Entry(cityPincode).Reference(cp => cp.City).LoadAsync(cancellationToken);
            if (cityPincode.City != null)
            {
                await _context.Entry(cityPincode.City).Reference(c => c.State).LoadAsync(cancellationToken);
            }

            var response = new MasterDataCityPincodeResponse
            {
                Id = cityPincode.Id,
                CityId = cityPincode.CityId,
                CityName = cityPincode.City?.Name ?? "Unknown",
                StateId = cityPincode.City?.StateId ?? 0,
                StateName = cityPincode.City?.State?.Name ?? "Unknown",
                Pincode = cityPincode.Pincode,
                IsActive = cityPincode.IsActive,
                CreatedAt = cityPincode.CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating city pincode: {ex.Message}");
        }
    }

    public async Task<ServiceResult> DeleteCityPincodeAsync(int cityPincodeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cityPincode = await _context.CityPincodes
                .FirstOrDefaultAsync(cp => cp.Id == cityPincodeId, cancellationToken);

            if (cityPincode == null)
            {
                return ServiceResult.NotFound("City pincode not found.");
            }

            // Deactivate instead of delete
            cityPincode.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = "City pincode deleted successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error deleting city pincode: {ex.Message}");
        }
    }

    // Verifications (all verifications for master admin)
    public async Task<ServiceResult> GetAllVerificationsAsync(string? status, CancellationToken cancellationToken = default)
    {
        try
        {
            var verificationsQuery = _context.ServiceProviderVerifications
                .AsNoTracking()
                .Where(v => v.IsActive)
                .Include(v => v.ProviderUser)
                    .ThenInclude(u => u.VerificationStatus)
                .Include(v => v.AssignedAdmin)
                .Include(v => v.VerifiedByUser);

            var allVerifications = await verificationsQuery
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);

            // Get verification status IDs
            var pendingStatusId = (int)VerificationStatusEnum.Pending;
            var approvedStatusId = (int)VerificationStatusEnum.Approved;
            var rejectedStatusId = (int)VerificationStatusEnum.Rejected;

            // Filter by status if provided
            List<ServiceProviderVerification> filteredVerifications;
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.ToLowerInvariant();
                filteredVerifications = normalizedStatus switch
                {
                    var s when s == VerificationStatusStrings.Pending => 
                        allVerifications.Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == pendingStatusId).ToList(),
                    var s when s == VerificationStatusStrings.Approved => 
                        allVerifications.Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == approvedStatusId).ToList(),
                    var s when s == VerificationStatusStrings.Rejected => 
                        allVerifications.Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == rejectedStatusId).ToList(),
                    var s when s == VerificationStatusStrings.UnderReview => 
                        allVerifications.Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == pendingStatusId && !string.IsNullOrEmpty(v.VerificationNotes)).ToList(),
                    _ => allVerifications
                };
            }
            else
            {
                filteredVerifications = allVerifications;
            }

            var providerUserIds = filteredVerifications.Select(v => v.ProviderUserId).ToList();

            // Get provider extra info
            var usersExtraInfo = await _context.UsersExtraInfos
                .AsNoTracking()
                .Where(uei => uei.UserId.HasValue && providerUserIds.Contains(uei.UserId.Value))
                .ToListAsync(cancellationToken);

            // Get provider addresses
            var addresses = await _context.UsersAddresses
                .AsNoTracking()
                .Where(ua => providerUserIds.Contains(ua.UserId.Value) && ua.IsActive)
                .Include(ua => ua.City)
                .Include(ua => ua.State)
                .ToListAsync(cancellationToken);

            // Get documents
            var documents = await _context.Documents
                .AsNoTracking()
                .Where(d => providerUserIds.Contains(d.UserId.Value) && d.IsActive)
                .ToListAsync(cancellationToken);

            var response = filteredVerifications.Select(v =>
            {
                var userExtraInfo = usersExtraInfo.FirstOrDefault(uei => uei.UserId == v.ProviderUserId);
                var address = addresses.FirstOrDefault(a => a.UserId == v.ProviderUserId);
                var userDocuments = documents.Where(d => d.UserId == v.ProviderUserId).ToList();

                var verificationStatus = VerificationStatusStrings.Pending;
                if (v.ProviderUser != null)
                {
                    if (v.ProviderUser.VerificationStatusId == approvedStatusId)
                        verificationStatus = VerificationStatusStrings.Approved;
                    else if (v.ProviderUser.VerificationStatusId == rejectedStatusId)
                        verificationStatus = VerificationStatusStrings.Rejected;
                    else if (v.ProviderUser.VerificationStatusId == pendingStatusId && !string.IsNullOrEmpty(v.VerificationNotes))
                        verificationStatus = VerificationStatusStrings.UnderReview;
                }

                return new ServiceProviderVerificationResponse
                {
                    Id = v.Id,
                    ProviderUserId = v.ProviderUserId,
                    AssignedAdminId = v.AssignedAdminId,
                    VerificationStatus = verificationStatus,
                    VerificationNotes = v.VerificationNotes,
                    RejectionReason = v.RejectionReason,
                    VerifiedAt = v.VerifiedAt,
                    VerifiedBy = v.VerifiedBy,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt,
                    ProviderName = userExtraInfo?.FullName ?? v.ProviderUser?.Name,
                    ProviderEmail = v.ProviderUser?.Email,
                    ProviderPhone = userExtraInfo?.PhoneNumber ?? v.ProviderUser?.MobileNumber,
                    ProviderAddress = address != null 
                        ? $"{address.AddressLine1} {address.AddressLine2} {address.Street}".Trim()
                        : null,
                    ProviderCity = address?.City?.Name,
                    ProviderState = address?.State?.Name,
                    Documents = userDocuments.Select(d => new VerificationDocumentResponse
                    {
                        Id = d.Id,
                        DocumentType = d.DocumentType,
                        FileUrl = d.FileUrl,
                        FileName = d.FileName,
                        UploadedAt = d.UploadedAt
                    }).ToList()
                };
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching verifications: {ex.Message}");
        }
    }

    // Helper method to convert IFormFile to FileUploadRequest and save
    private async Task<string?> SaveFileAsync(IFormFile? file, string subfolder, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        var fileRequest = new FileUploadRequest
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            OpenReadStream = file.OpenReadStream
        };

        var result = await _fileStorageService.SaveAsync(fileRequest, subfolder, cancellationToken);
        return result.RelativePath;
    }

    // Bookings (all bookings for master admin)
    public async Task<ServiceResult> GetAllBookingsAsync(List<string>? statuses, CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<BookingRequest> bookingsQuery = _context.BookingRequests
                .AsNoTracking()
                .Include(b => b.Service)
                    .ThenInclude(s => s.Category)
                .Include(b => b.ServiceProvider)
                .Include(b => b.Customer)
                .Include(b => b.Admin)
                .Include(b => b.StatusNavigation);

            if (statuses != null && statuses.Count > 0)
            {
                var statusIds = await _context.BookingStatuses
                    .Where(s => statuses.Contains(s.Code) && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                if (statusIds.Count > 0)
                {
                    bookingsQuery = bookingsQuery.Where(b => statusIds.Contains(b.StatusId));
                }
            }

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            var response = bookings.Select(b => new BookingRequestDto
            {
                Id = b.Id,
                CustomerId = b.CustomerId,
                ServiceProviderId = b.ServiceProviderId,
                ServiceId = b.ServiceId,
                Pincode = b.Pincode ?? string.Empty,
                AdminId = b.AdminId,
                Status = b.StatusNavigation?.Code ?? "UNKNOWN",
                CustomerAddress = b.CustomerAddress,
                RequestDescription = b.RequestDescription,
                PreferredDate = b.PreferredDate,
                PreferredTime = b.PreferredTime,
                TimeSlot = b.TimeSlot,
                EstimatedPrice = b.EstimatedPrice,
                FinalPrice = b.FinalPrice,
                AdminNotes = b.AdminNotes,
                ServiceProviderNotes = b.ServiceProviderNotes,
                CustomerRating = b.CustomerRating,
                CustomerFeedback = b.CustomerFeedback,
                AssignedAt = b.AssignedAt,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                WorkingHours = b.WorkingHours,
                ServiceTypeId = b.ServiceTypeId,
                AddressLine1 = b.AddressLine1,
                AddressLine2 = b.AddressLine2,
                City = b.City,
                State = b.State,
                CustomerPhone = b.CustomerPhone,
                AlternativeMobileNumber = b.AlternativeMobileNumber,
                CustomerName = b.CustomerName,
                Service = b.Service != null ? new BookingServiceDto
                {
                    Id = b.Service.Id,
                    ServiceName = b.Service.ServiceName,
                    Description = b.Service.Description,
                    CategoryId = b.Service.CategoryId ?? 0
                } : null,
                Customer = b.Customer != null ? new BookingUserDto
                {
                    Id = b.Customer.Id,
                    Name = b.Customer.Name,
                    Email = b.Customer.Email,
                    Phone = b.Customer.MobileNumber
                } : null,
                ServiceProvider = b.ServiceProvider != null ? new BookingServiceProviderDto
                {
                    Id = b.ServiceProvider.Id,
                    Name = b.ServiceProvider.Name,
                    Email = b.ServiceProvider.Email,
                    Phone = b.ServiceProvider.MobileNumber
                } : null,
                Admin = b.Admin != null ? new BookingAdminDto
                {
                    Id = b.Admin.Id,
                    Name = b.Admin.Name,
                    Email = b.Admin.Email
                } : null
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching bookings: {ex.Message}");
        }
    }
}
