using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class LeaveResponse
{
    public Guid Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeaveDayResponse
{
    public DateTime Date { get; set; }
    public string? Description { get; set; }
}

public class LeavesListResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<LeaveResponse> Leaves { get; set; } = new();
    public List<LeaveDayResponse> LeaveDays { get; set; } = new();
}

public class BookingReassignmentResponse
{
    public Guid BookingId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public string Reason { get; set; } = string.Empty; // "Service provider on leave"
}

public class BookingsForReassignmentResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<BookingReassignmentResponse> Bookings { get; set; } = new();
}
