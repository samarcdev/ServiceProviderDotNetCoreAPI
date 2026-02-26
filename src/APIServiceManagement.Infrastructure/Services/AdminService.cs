using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
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

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> GetDashboardStatsAsync(Guid? adminId, CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        // Verify user has admin role
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId.Value, cancellationToken);

        if (user == null)
        {
            return ServiceResult.NotFound("User not found.");
        }

        var roleName = user.Role?.Name?.ToLowerInvariant() ?? string.Empty;
        var normalizedRoleName = roleName.Replace("_", "").Replace("-", "");
        var isAdmin = normalizedRoleName == RoleNames.Normalized.Admin 
            || normalizedRoleName == RoleNames.Normalized.MasterAdmin 
            || normalizedRoleName == RoleNames.Normalized.DefaultAdmin 
            || normalizedRoleName == RoleNames.Normalized.SuperAdmin;

        if (!isAdmin)
        {
            return ServiceResult.Forbidden("Access denied. Admin role required.");
        }

        // Check if admin is a super admin (DefaultAdmin, SuperAdmin, or MasterAdmin)
        var isSuperAdmin = normalizedRoleName == RoleNames.Normalized.DefaultAdmin 
            || normalizedRoleName == RoleNames.Normalized.SuperAdmin 
            || normalizedRoleName == RoleNames.Normalized.MasterAdmin;

        try
        {
            // Initialize stats response
            var stats = new AdminDashboardStatsResponse();

            // Get verification counts for assigned admin (always calculate these)
            var verifications = await _context.ServiceProviderVerifications
                .AsNoTracking()
                .Where(v => v.AssignedAdminId == adminId.Value && v.IsActive)
                .Include(v => v.ProviderUser)
                .ToListAsync(cancellationToken);

            // Get verification status IDs from enum
            var pendingStatusId = (int)VerificationStatusEnum.Pending;
            var approvedStatusId = (int)VerificationStatusEnum.Approved;
            var rejectedStatusId = (int)VerificationStatusEnum.Rejected;

            // Calculate verification stats
            stats.TotalRequests = verifications.Count;
            stats.PendingRequests = verifications.Count(v => 
                v.ProviderUser != null && v.ProviderUser.VerificationStatusId == pendingStatusId);
            stats.VerifiedRequests = verifications.Count(v => 
                v.ProviderUser != null && v.ProviderUser.VerificationStatusId == approvedStatusId);
            stats.RejectedRequests = verifications.Count(v => 
                v.ProviderUser != null && v.ProviderUser.VerificationStatusId == rejectedStatusId);

            // Get state IDs for booking stats
            // Super admins (DefaultAdmin, SuperAdmin, MasterAdmin) see all bookings
            // Regular admins only see bookings in their assigned states
            List<int> stateIds;
            if (isSuperAdmin)
            {
                // Get all active states for super admins
                stateIds = await _context.States
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // Get assigned states for regular admins
                stateIds = await _context.AdminStateAssignments
                    .AsNoTracking()
                    .Where(assignment => assignment.AdminUserId == adminId.Value)
                    .Select(assignment => assignment.StateId)
                    .ToListAsync(cancellationToken);
            }

            // Calculate booking stats if we have states (super admins will always have states)
            if (stateIds.Count > 0)
            {
                // Get cities in assigned states
                var cityIds = await _context.Cities
                    .AsNoTracking()
                    .Where(c => c.StateId.HasValue && stateIds.Contains(c.StateId.Value) && c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);

                // Get pincodes for cities in assigned states
                var pincodes = await _context.CityPincodes
                    .AsNoTracking()
                    .Where(cp => cp.IsActive && cityIds.Contains(cp.CityId))
                    .Select(cp => cp.Pincode)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (pincodes.Count > 0)
                {
                    // Get booking status IDs by codes
                    var statusIds = await GetStatusIdsByCodesAsync(new[] { 
                        BookingStatusCodes.Pending, 
                        BookingStatusCodes.Assigned, 
                        BookingStatusCodes.InProgress, 
                        BookingStatusCodes.Completed 
                    }, cancellationToken);
                    
                    // Get status IDs (dictionary keys are normalized to uppercase)
                    var pendingBookingStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Pending);
                    var assignedBookingStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Assigned);
                    var inProgressBookingStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.InProgress);
                    var completedBookingStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Completed);

                    // Get all bookings in assigned pincodes and count by status
                    var bookings = await _context.BookingRequests
                        .AsNoTracking()
                        .Where(b => b.AdminId== adminId&& pincodes.Contains(b.Pincode))
                        .Select(b => b.StatusId)
                        .ToListAsync(cancellationToken);

                    // Count bookings by status ID (only if status ID is valid)
                    if (pendingBookingStatusId > 0)
                    {
                        stats.PendingBookings = bookings.Count(s => s == pendingBookingStatusId);
                    }
                    if (assignedBookingStatusId > 0)
                    {
                        stats.AssignedBookings = bookings.Count(s => s == assignedBookingStatusId);
                    }
                    if (inProgressBookingStatusId > 0)
                    {
                        stats.InProgressBookings = bookings.Count(s => s == inProgressBookingStatusId);
                    }
                    if (completedBookingStatusId > 0)
                    {
                        stats.CompletedBookings = bookings.Count(s => s == completedBookingStatusId);
                    }
                }
            }

            return ServiceResult.Ok(stats);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching dashboard stats: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetVerificationsByStatusAsync(Guid? adminId, string? status, CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        // Verify user has admin role
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId.Value, cancellationToken);

        if (user == null)
        {
            return ServiceResult.NotFound("User not found.");
        }

        var roleName = user.Role?.Name?.ToLowerInvariant() ?? string.Empty;
        var normalizedRoleName = roleName.Replace("_", "").Replace("-", "");
        var isAdmin = normalizedRoleName == RoleNames.Normalized.Admin 
            || normalizedRoleName == RoleNames.Normalized.MasterAdmin 
            || normalizedRoleName == RoleNames.Normalized.DefaultAdmin 
            || normalizedRoleName == RoleNames.Normalized.SuperAdmin;

        if (!isAdmin)
        {
            return ServiceResult.Forbidden("Access denied. Admin role required.");
        }

        try
        {
            // Get all verifications for the admin
            var verificationsQuery = _context.ServiceProviderVerifications
                .AsNoTracking()
                .Where(v => v.AssignedAdminId == adminId.Value && v.IsActive)
                .Include(v => v.ProviderUser)
                    .ThenInclude(u => u.VerificationStatus)
                .Include(v => v.AssignedAdmin);

            var allVerifications = await verificationsQuery
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync(cancellationToken);

            // Get verification status IDs from enum
            var pendingStatusId = (int)VerificationStatusEnum.Pending;
            var approvedStatusId = (int)VerificationStatusEnum.Approved;
            var rejectedStatusId = (int)VerificationStatusEnum.Rejected;

            // Filter verifications based on User.VerificationStatusId
            List<ServiceProviderVerification> filteredVerifications;
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = status.ToLowerInvariant();
                
                if (normalizedStatus == VerificationStatusStrings.Pending)
                {
                    filteredVerifications = allVerifications
                        .Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == pendingStatusId)
                        .ToList();
                }
                else if (normalizedStatus == VerificationStatusStrings.Approved)
                {
                    filteredVerifications = allVerifications
                        .Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == approvedStatusId)
                        .ToList();
                }
                else if (normalizedStatus == VerificationStatusStrings.Rejected)
                {
                    filteredVerifications = allVerifications
                        .Where(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == rejectedStatusId)
                        .ToList();
                }
                else if (normalizedStatus == VerificationStatusStrings.UnderReview)
                {
                    // Under review is treated as pending with notes
                    filteredVerifications = allVerifications
                        .Where(v => v.ProviderUser != null && 
                            v.ProviderUser.VerificationStatusId == pendingStatusId &&
                            !string.IsNullOrEmpty(v.VerificationNotes))
                        .ToList();
                }
                else
                {
                    return ServiceResult.BadRequest($"Invalid status. Valid values: {VerificationStatusStrings.Pending}, {VerificationStatusStrings.Approved}, {VerificationStatusStrings.Rejected}, {VerificationStatusStrings.UnderReview}");
                }
            }
            else
            {
                // If no status filter, return all verifications
                filteredVerifications = allVerifications;
            }

            // Calculate batch counts from all verifications (not filtered)
            var pendingCount = allVerifications.Count(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == pendingStatusId);
            var approvedCount = allVerifications.Count(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == approvedStatusId);
            var rejectedCount = allVerifications.Count(v => v.ProviderUser != null && v.ProviderUser.VerificationStatusId == rejectedStatusId);

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

            var verificationResponses = filteredVerifications.Select(v =>
            {
                var userExtraInfo = usersExtraInfo.FirstOrDefault(uei => uei.UserId == v.ProviderUserId);
                var address = addresses.FirstOrDefault(a => a.UserId == v.ProviderUserId);
                var userDocuments = documents.Where(d => d.UserId == v.ProviderUserId).ToList();

                // Determine verification status from User.VerificationStatusId
                var verificationStatus = VerificationStatusStrings.Pending;
                if (v.ProviderUser != null)
                {
                    if (v.ProviderUser.VerificationStatusId == approvedStatusId)
                        verificationStatus = VerificationStatusStrings.Approved;
                    else if (v.ProviderUser.VerificationStatusId == rejectedStatusId)
                        verificationStatus = VerificationStatusStrings.Rejected;
                    else if (v.ProviderUser.VerificationStatusId == pendingStatusId && !string.IsNullOrEmpty(v.VerificationNotes))
                        verificationStatus = VerificationStatusStrings.UnderReview;
                    else
                        verificationStatus = VerificationStatusStrings.Pending;
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

            // Return response with batch counts
            var response = new ServiceProviderVerificationsWithBatchCountsResponse
            {
                PendingCount = pendingCount,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount,
                Verifications = verificationResponses
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching verifications: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetVerificationDetailsAsync(int verificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var verification = await _context.ServiceProviderVerifications
                .AsNoTracking()
                .Include(v => v.ProviderUser)
                    .ThenInclude(u => u.VerificationStatus)
                .Include(v => v.AssignedAdmin)
                .Include(v => v.VerifiedByUser)
                .FirstOrDefaultAsync(v => v.Id == verificationId && v.IsActive, cancellationToken);

            if (verification == null)
            {
                return ServiceResult.NotFound("Verification not found.");
            }

            // Get provider extra info
            var userExtraInfo = await _context.UsersExtraInfos
                .AsNoTracking()
                .FirstOrDefaultAsync(uei => uei.UserId == verification.ProviderUserId, cancellationToken);

            // Get provider address
            var address = await _context.UsersAddresses
                .AsNoTracking()
                .Where(ua => ua.UserId == verification.ProviderUserId && ua.IsActive)
                .Include(ua => ua.City)
                .Include(ua => ua.State)
                .FirstOrDefaultAsync(cancellationToken);

            // Get documents
            var documents = await _context.Documents
                .AsNoTracking()
                .Where(d => d.UserId == verification.ProviderUserId && d.IsActive)
                .ToListAsync(cancellationToken);

            // Get verification status IDs from enum
            var pendingStatusId = (int)VerificationStatusEnum.Pending;
            var approvedStatusId = (int)VerificationStatusEnum.Approved;
            var rejectedStatusId = (int)VerificationStatusEnum.Rejected;

            // Determine verification status from User.VerificationStatusId
            var verificationStatus = VerificationStatusStrings.Pending;
            if (verification.ProviderUser != null)
            {
                if (verification.ProviderUser.VerificationStatusId == approvedStatusId)
                    verificationStatus = VerificationStatusStrings.Approved;
                else if (verification.ProviderUser.VerificationStatusId == rejectedStatusId)
                    verificationStatus = VerificationStatusStrings.Rejected;
                else if (verification.ProviderUser.VerificationStatusId == pendingStatusId && !string.IsNullOrEmpty(verification.VerificationNotes))
                    verificationStatus = VerificationStatusStrings.UnderReview;
                else
                    verificationStatus = VerificationStatusStrings.Pending;
            }

            var response = new ServiceProviderVerificationResponse
            {
                Id = verification.Id,
                ProviderUserId = verification.ProviderUserId,
                AssignedAdminId = verification.AssignedAdminId,
                VerificationStatus = verificationStatus,
                VerificationNotes = verification.VerificationNotes,
                RejectionReason = verification.RejectionReason,
                VerifiedAt = verification.VerifiedAt,
                VerifiedBy = verification.VerifiedBy,
                CreatedAt = verification.CreatedAt,
                UpdatedAt = verification.UpdatedAt,
                ProviderName = userExtraInfo?.FullName ?? verification.ProviderUser?.Name,
                ProviderEmail = verification.ProviderUser?.Email,
                ProviderPhone = userExtraInfo?.PhoneNumber ?? verification.ProviderUser?.MobileNumber,
                ProviderAddress = address != null 
                    ? $"{address.AddressLine1} {address.AddressLine2} {address.Street}".Trim()
                    : null,
                ProviderCity = address?.City?.Name,
                ProviderState = address?.State?.Name,
                Documents = documents.Select(d => new VerificationDocumentResponse
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType,
                    FileUrl = d.FileUrl,
                    FileName = d.FileName,
                    UploadedAt = d.UploadedAt
                }).ToList()
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching verification details: {ex.Message}");
        }
    }

    public async Task<ServiceResult> UpdateVerificationStatusAsync(
        Guid? adminId, 
        int verificationId, 
        UpdateVerificationStatusRequest request, 
        CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return ServiceResult.BadRequest("Status is required.");
        }

        var normalizedStatus = request.Status.ToLowerInvariant();
        if (!new[] { VerificationStatusStrings.Approved, VerificationStatusStrings.Rejected, VerificationStatusStrings.UnderReview }.Contains(normalizedStatus))
        {
            return ServiceResult.BadRequest($"Invalid status. Valid values: {VerificationStatusStrings.Approved}, {VerificationStatusStrings.Rejected}, {VerificationStatusStrings.UnderReview}");
        }

        if (normalizedStatus == VerificationStatusStrings.Rejected && string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return ServiceResult.BadRequest("Rejection reason is required when rejecting a verification.");
        }

        try
        {
            var verification = await _context.ServiceProviderVerifications
                .Include(v => v.ProviderUser)
                .FirstOrDefaultAsync(v => v.Id == verificationId && v.IsActive, cancellationToken);

            if (verification == null)
            {
                return ServiceResult.NotFound("Verification not found.");
            }

            // Check if admin is assigned to this verification
            if (verification.AssignedAdminId != adminId.Value)
            {
                return ServiceResult.Forbidden("You are not assigned to this verification.");
            }

            var now = DateTime.UtcNow;

            // Update verification record
            verification.VerificationNotes = request.Notes;
            verification.RejectionReason = normalizedStatus == VerificationStatusStrings.Rejected ? request.RejectionReason : null;
            verification.VerifiedAt = normalizedStatus == VerificationStatusStrings.Approved ? now : (DateTime?)null;
            verification.VerifiedBy = normalizedStatus == VerificationStatusStrings.Approved ? adminId.Value : (Guid?)null;
            verification.UpdatedAt = now;

            // Update user verification status
            var user = verification.ProviderUser;
            if (user != null)
            {
                var verificationStatusId = normalizedStatus switch
                {
                    var s when s == VerificationStatusStrings.Approved => (int)VerificationStatusEnum.Approved,
                    var s when s == VerificationStatusStrings.Rejected => (int)VerificationStatusEnum.Rejected,
                    var s when s == VerificationStatusStrings.UnderReview => (int)VerificationStatusEnum.Pending,
                    _ => (int)VerificationStatusEnum.Pending
                };
                user.VerificationStatusId = verificationStatusId;
                user.UpdatedAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse 
            { 
                Success = true, 
                Message = $"Verification {normalizedStatus} successfully." 
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error updating verification status: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CheckAdminPermissionsAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            if (user == null)
            {
                return ServiceResult.NotFound("User not found.");
            }

            var roleName = user.Role?.Name?.ToLowerInvariant() ?? string.Empty;
            // Normalize role name: handle both "MasterAdmin" -> "masteradmin" and "super_admin" -> "superadmin"
            var normalizedRoleName = roleName.Replace("_", "").Replace("-", "");
            // Check for admin roles using constants
            var isAdmin = normalizedRoleName == RoleNames.Normalized.Admin 
                || normalizedRoleName == RoleNames.Normalized.MasterAdmin 
                || normalizedRoleName == RoleNames.Normalized.DefaultAdmin 
                || normalizedRoleName == RoleNames.Normalized.SuperAdmin;
            var isSuperAdmin = normalizedRoleName == RoleNames.Normalized.SuperAdmin 
                || normalizedRoleName == RoleNames.Normalized.MasterAdmin;

            var response = new AdminPermissionResponse
            {
                IsAdmin = isAdmin,
                IsSuperAdmin = isSuperAdmin,
                RoleName = user.Role?.Name
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error checking admin permissions: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetAdminAssignedStatesAsync(Guid? adminId, CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        try
        {
            var assignments = await _context.AdminStateAssignments
                .AsNoTracking()
                .Where(assignment => assignment.AdminUserId == adminId.Value)
                .Include(assignment => assignment.State)
                .OrderByDescending(assignment => assignment.AssignedAt)
                .ToListAsync(cancellationToken);

            var response = assignments.Select(a => new AdminAssignedStateResponse
            {
                Id = a.Id,
                StateId = a.StateId,
                StateName = a.State?.Name ?? "Unknown"
            }).ToList();

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching assigned states: {ex.Message}");
        }
    }

    public async Task<ServiceResult> TerminateUserAsync(Guid? adminId, TerminateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (request == null || request.UserId == Guid.Empty)
        {
            return ServiceResult.BadRequest("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
        {
            return ServiceResult.BadRequest("A solid reason for termination is required (minimum 10 characters).");
        }

        // Verify the requesting user has admin role
        var adminUser = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId.Value, cancellationToken);

        if (adminUser == null)
        {
            return ServiceResult.NotFound("Admin user not found.");
        }

        var roleName = adminUser.Role?.Name?.ToLowerInvariant() ?? string.Empty;
        var normalizedRoleName = roleName.Replace("_", "").Replace("-", "");
        var isAdmin = normalizedRoleName == RoleNames.Normalized.Admin
            || normalizedRoleName == RoleNames.Normalized.MasterAdmin
            || normalizedRoleName == RoleNames.Normalized.DefaultAdmin
            || normalizedRoleName == RoleNames.Normalized.SuperAdmin;

        if (!isAdmin)
        {
            return ServiceResult.Forbidden("Access denied. Admin role required.");
        }

        try
        {
            // Get the target user
            var targetUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (targetUser == null)
            {
                return ServiceResult.NotFound("User not found.");
            }

            // Prevent terminating yourself
            if (targetUser.Id == adminId.Value)
            {
                return ServiceResult.BadRequest("You cannot terminate your own account.");
            }

            // Check if the user is already terminated
            if (targetUser.StatusId == (int)UserStatusEnum.Terminated)
            {
                return ServiceResult.BadRequest("This user is already terminated.");
            }

            var now = DateTime.UtcNow;

            // Update user status to Terminated
            targetUser.StatusId = (int)UserStatusEnum.Terminated;
            targetUser.RefreshToken = null;
            targetUser.RefreshTokenExpiresAt = null;
            targetUser.UpdatedAt = now;

            // Create termination record
            var termination = new UserTermination
            {
                UserId = request.UserId,
                TerminatedBy = adminId.Value,
                Reason = request.Reason.Trim(),
                TerminatedAt = now,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.UserTerminations.Add(termination);
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new OperationResponse
            {
                Success = true,
                Message = $"User '{targetUser.Name}' has been terminated successfully."
            });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error terminating user: {ex.Message}");
        }
    }

    private async Task<Dictionary<string, int>> GetStatusIdsByCodesAsync(string[] codes, CancellationToken cancellationToken = default)
    {
        var normalizedCodes = codes.Select(c => c.ToUpperInvariant()).ToArray();
        var statuses = await _context.BookingStatuses
            .AsNoTracking()
            .Where(s => normalizedCodes.Contains(s.Code) && s.IsActive)
            .ToListAsync(cancellationToken);
        
        // Normalize dictionary keys to uppercase to match constants (in case database has mixed case)
        return statuses.ToDictionary(s => s.Code.ToUpperInvariant(), s => s.Id);
    }
}
