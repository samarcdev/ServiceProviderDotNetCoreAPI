using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class WeeklyRevenueDayDto
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ServiceProviderPerformanceDto
{
    public int JobsThisPeriod { get; set; }
    public int JobsPreviousPeriod { get; set; }
    public decimal PercentChange { get; set; }
    public decimal AverageRating { get; set; }
}

public class ServiceProviderKpiDto
{
    public int Current { get; set; }
    public int Previous { get; set; }
    public decimal PercentChange { get; set; }
}

public class ServiceProviderRevenueKpiDto
{
    public decimal Current { get; set; }
    public decimal Previous { get; set; }
    public decimal PercentChange { get; set; }
}

public class CustomerDashboardResponse
{
    public string UserPincode { get; set; } = string.Empty;
    public List<ServiceAvailabilityItem> AvailableServices { get; set; } = new();
    public List<BookingRequestDto> RecentBookings { get; set; } = new();
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
}

public class AdminBookingDashboardResponse
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int AssignedRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int HoldRequests { get; set; }
    public int CompletedRequests { get; set; }
    public List<BookingRequestDto> RecentRequests { get; set; } = new();
    public List<PincodeStatResponse> PincodeStats { get; set; } = new();
}

public class ServiceProviderDashboardResponse
{
    public List<BookingRequestDto> AssignedBookings { get; set; } = new();
    public List<BookingRequestDto> CompletedBookings { get; set; } = new();
    public int TotalAssignments { get; set; }
    public int PendingAssignments { get; set; }
    public int CompletedAssignments { get; set; }

    // Extended dashboard data
    public ServiceProviderKpiDto? TotalJobVolume { get; set; }
    public ServiceProviderRevenueKpiDto? TotalRevenue { get; set; }
    public ServiceProviderKpiDto? CompletedJobs { get; set; }
    public ServiceProviderKpiDto? PendingJobs { get; set; }
    public List<BookingRequestDto> TodaysSchedule { get; set; } = new();
    public List<WeeklyRevenueDayDto> WeeklyRevenue { get; set; } = new();
    public ServiceProviderPerformanceDto? YourPerformance { get; set; }
}

public class PincodeStatResponse
{
    public string Pincode { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
}
