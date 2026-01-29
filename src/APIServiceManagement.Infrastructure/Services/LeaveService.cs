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

public class LeaveService : ILeaveService
{
    private readonly AppDbContext _context;

    public LeaveService(AppDbContext context)
    {
        _context = context;
    }

    private async Task<int> GetStatusIdByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var status = await _context.BookingStatuses
            .AsNoTracking()
            .Where(s => s.Code == code && s.IsActive == true)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (status == null)
        {
            throw new InvalidOperationException($"Booking status with code '{code}' not found.");
        }
        
        return status.Id;
    }

    public async Task<ServiceResult> ApplyLeaveAsync(Guid? serviceProviderId, ApplyLeaveRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!serviceProviderId.HasValue)
            {
                return ServiceResult.BadRequest("Service provider ID is required.");
            }

            // Normalize dates to UTC first - ensure they're date-only values with UTC kind
            // IMPORTANT: Don't use .Date property as it returns DateTime with Kind=Unspecified
            // Instead, create new DateTime with UTC kind directly
            DateTime startDate;
            if (request.StartDate.Kind == DateTimeKind.Utc)
            {
                startDate = new DateTime(request.StartDate.Year, request.StartDate.Month, request.StartDate.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (request.StartDate.Kind == DateTimeKind.Local)
            {
                var utc = request.StartDate.ToUniversalTime();
                startDate = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                // Unspecified - assume it's already in UTC date format
                startDate = new DateTime(request.StartDate.Year, request.StartDate.Month, request.StartDate.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            
            DateTime endDate;
            if (request.EndDate.Kind == DateTimeKind.Utc)
            {
                endDate = new DateTime(request.EndDate.Year, request.EndDate.Month, request.EndDate.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (request.EndDate.Kind == DateTimeKind.Local)
            {
                var utc = request.EndDate.ToUniversalTime();
                endDate = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                // Unspecified - assume it's already in UTC date format
                endDate = new DateTime(request.EndDate.Year, request.EndDate.Month, request.EndDate.Day, 0, 0, 0, DateTimeKind.Utc);
            }

            if (endDate < startDate)
            {
                return ServiceResult.BadRequest("End date must be greater than or equal to start date.");
            }

            // Check for overlapping leaves using normalized UTC dates
            var overlappingLeaves = await _context.ServiceProviderLeaveDays
                .Where(l => l.ServiceProviderId == serviceProviderId.Value
                    && l.LeaveDate >= startDate
                    && l.LeaveDate <= endDate)
                .AnyAsync(cancellationToken);

            if (overlappingLeaves)
            {
                return ServiceResult.BadRequest("You already have a leave for this date range.");
            }

            // Create leave days
            var leaveDays = new List<ServiceProviderLeaveDay>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                leaveDays.Add(new ServiceProviderLeaveDay
                {
                    ServiceProviderId = serviceProviderId.Value,
                    LeaveDate = date,
                    Description = request.Description
                });
            }

            _context.ServiceProviderLeaveDays.AddRange(leaveDays);
            await _context.SaveChangesAsync(cancellationToken);

            // Mark existing booking assignments as unassigned due to leave
            // Load all assignments first, then filter in memory to avoid DateTime comparison issues
            var allAssignments = await _context.BookingAssignments
                .Include(ba => ba.BookingRequest)
                .Where(ba => ba.ServiceProviderId == serviceProviderId.Value
                    && ba.IsCurrent
                    && ba.BookingRequest.PreferredDate.HasValue)
                .ToListAsync(cancellationToken);
            
            // Filter in memory comparing date parts only (avoiding .Date property which loses Kind)
            var affectedAssignments = allAssignments
                .Where(ba => 
                {
                    if (!ba.BookingRequest.PreferredDate.HasValue) return false;
                    var preferredDate = ba.BookingRequest.PreferredDate.Value;
                    
                    // Create UTC DateTime for comparison (preserving UTC kind)
                    var prefDateUtc = preferredDate.Kind == DateTimeKind.Utc
                        ? preferredDate
                        : preferredDate.Kind == DateTimeKind.Local
                            ? preferredDate.ToUniversalTime()
                            : DateTime.SpecifyKind(preferredDate, DateTimeKind.Utc);
                    
                    var prefDateOnly = new DateTime(prefDateUtc.Year, prefDateUtc.Month, prefDateUtc.Day, 0, 0, 0, DateTimeKind.Utc);
                    
                    return prefDateOnly >= startDate && prefDateOnly <= endDate;
                })
                .ToList();

            foreach (var assignment in affectedAssignments)
            {
                assignment.IsCurrent = false;
                assignment.UnassignedAt = DateTime.UtcNow;
                assignment.UnassignedReason = $"Service provider on leave from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}" + 
                    (string.IsNullOrWhiteSpace(request.Description) ? "" : $". {request.Description}");
                assignment.ReasonType = "leave";
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Get bookings that need reassignment using normalized UTC dates
            var bookingsForReassignment = await GetBookingsForReassignmentAsync(
                serviceProviderId,
                startDate,
                endDate,
                cancellationToken);

            var response = new LeaveResponse
            {
                Id = leaveDays.First().Id,
                StartDate = startDate,
                EndDate = endDate,
                Description = request.Description,
                CreatedAt = leaveDays.First().CreatedAt
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error applying leave: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetLeavesAsync(Guid? serviceProviderId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!serviceProviderId.HasValue)
            {
                return ServiceResult.BadRequest("Service provider ID is required.");
            }

            var leaveDays = await _context.ServiceProviderLeaveDays
                .Where(l => l.ServiceProviderId == serviceProviderId.Value)
                .OrderBy(l => l.LeaveDate)
                .ToListAsync(cancellationToken);

            var leaves = new List<LeaveResponse>();
            if (leaveDays.Count > 0)
            {
                ServiceProviderLeaveDay rangeStart = leaveDays[0];
                ServiceProviderLeaveDay rangeEnd = leaveDays[0];

                for (var i = 1; i < leaveDays.Count; i++)
                {
                    var current = leaveDays[i];
                    var isSameDescription = string.Equals(current.Description, rangeStart.Description, StringComparison.Ordinal);
                    var isNextDay = current.LeaveDate.Date == rangeEnd.LeaveDate.Date.AddDays(1);

                    if (isNextDay && isSameDescription)
                    {
                        rangeEnd = current;
                        continue;
                    }

                    leaves.Add(new LeaveResponse
                    {
                        Id = rangeStart.Id,
                        StartDate = rangeStart.LeaveDate,
                        EndDate = rangeEnd.LeaveDate,
                        Description = rangeStart.Description,
                        CreatedAt = rangeStart.CreatedAt
                    });

                    rangeStart = current;
                    rangeEnd = current;
                }

                leaves.Add(new LeaveResponse
                {
                    Id = rangeStart.Id,
                    StartDate = rangeStart.LeaveDate,
                    EndDate = rangeEnd.LeaveDate,
                    Description = rangeStart.Description,
                    CreatedAt = rangeStart.CreatedAt
                });
            }

            var response = new LeavesListResponse
            {
                Success = true,
                Leaves = leaves,
                LeaveDays = leaveDays.Select(l => new LeaveDayResponse
                {
                    Date = l.LeaveDate,
                    Description = l.Description
                }).ToList()
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching leaves: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetBookingsForReassignmentAsync(
        Guid? serviceProviderId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!serviceProviderId.HasValue)
            {
                return ServiceResult.BadRequest("Service provider ID is required.");
            }

            // Ensure dates are UTC (they should be from ApplyLeaveAsync)
            var startDateUtc = startDate.Kind == DateTimeKind.Utc
                ? startDate
                : startDate.Kind == DateTimeKind.Local
                    ? startDate.ToUniversalTime()
                    : DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
            
            var endDateUtc = endDate.Kind == DateTimeKind.Utc
                ? endDate
                : endDate.Kind == DateTimeKind.Local
                    ? endDate.ToUniversalTime()
                    : DateTime.SpecifyKind(endDate, DateTimeKind.Utc);

            // Get status IDs for filtering
            var assignedStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Assigned, cancellationToken);
            var inProgressStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.InProgress, cancellationToken);
            var pendingStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Pending, cancellationToken);

            // Get bookings assigned to this service provider that fall within the leave period
            // Load all bookings first, then filter in memory to avoid DateTime comparison issues
            var allBookings = await _context.BookingRequests
                .Include(b => b.Service)
                .Where(b => b.ServiceProviderId == serviceProviderId.Value
                    && b.PreferredDate.HasValue
                    && (b.StatusId == assignedStatusId
                        || b.StatusId == inProgressStatusId
                        || b.StatusId == pendingStatusId))
                .ToListAsync(cancellationToken);

            // Filter in memory comparing date parts only
            var bookings = allBookings
                .Where(b =>
                {
                    if (!b.PreferredDate.HasValue) return false;
                    var preferredDate = b.PreferredDate.Value;
                    
                    // Create UTC DateTime for comparison
                    var prefDateUtc = preferredDate.Kind == DateTimeKind.Utc
                        ? preferredDate
                        : preferredDate.Kind == DateTimeKind.Local
                            ? preferredDate.ToUniversalTime()
                            : DateTime.SpecifyKind(preferredDate, DateTimeKind.Utc);
                    
                    var prefDateOnly = new DateTime(prefDateUtc.Year, prefDateUtc.Month, prefDateUtc.Day, 0, 0, 0, DateTimeKind.Utc);
                    
                    return prefDateOnly >= startDateUtc && prefDateOnly <= endDateUtc;
                })
                .Select(b => new BookingReassignmentResponse
                {
                    BookingId = b.Id,
                    CustomerName = b.CustomerName,
                    ServiceName = b.Service != null ? b.Service.ServiceName : "Unknown",
                    Pincode = b.Pincode,
                    PreferredDate = b.PreferredDate,
                    Reason = "Service provider on leave"
                })
                .ToList();

            var response = new BookingsForReassignmentResponse
            {
                Success = true,
                Bookings = bookings
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error fetching bookings for reassignment: {ex.Message}");
        }
    }

    public async Task<ServiceResult> IsServiceProviderOnLeaveAsync(Guid? serviceProviderId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!serviceProviderId.HasValue)
            {
                return ServiceResult.BadRequest("Service provider ID is required.");
            }

            // Normalize date to UTC (avoid using .Date which returns Kind=Unspecified)
            DateTime dateUtc;
            if (date.Kind == DateTimeKind.Utc)
            {
                dateUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (date.Kind == DateTimeKind.Local)
            {
                var utc = date.ToUniversalTime();
                dateUtc = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                dateUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }

            var isOnLeave = await _context.ServiceProviderLeaveDays
                .AnyAsync(l => l.ServiceProviderId == serviceProviderId.Value && l.LeaveDate.Date == dateUtc.Date, cancellationToken);

            return ServiceResult.Ok(new { IsOnLeave = isOnLeave });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error checking leave status: {ex.Message}");
        }
    }

    public async Task<ServiceResult> CancelLeaveDayAsync(Guid? serviceProviderId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!serviceProviderId.HasValue)
            {
                return ServiceResult.BadRequest("Service provider ID is required.");
            }

            DateTime dateUtc;
            if (date.Kind == DateTimeKind.Utc)
            {
                dateUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else if (date.Kind == DateTimeKind.Local)
            {
                var utc = date.ToUniversalTime();
                dateUtc = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
            }
            else
            {
                dateUtc = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }

            var leaveDay = await _context.ServiceProviderLeaveDays
                .FirstOrDefaultAsync(l => l.ServiceProviderId == serviceProviderId.Value && l.LeaveDate.Date == dateUtc.Date, cancellationToken);

            if (leaveDay == null)
            {
                return ServiceResult.NotFound("Leave day not found.");
            }

            _context.ServiceProviderLeaveDays.Remove(leaveDay);
            await _context.SaveChangesAsync(cancellationToken);

            return ServiceResult.Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Error cancelling leave day: {ex.Message}");
        }
    }
}
