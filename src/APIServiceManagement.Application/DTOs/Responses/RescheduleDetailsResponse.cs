using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class RescheduleDetailsResponse
{
    public Guid BookingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminReason { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? CurrentPreferredDate { get; set; }
    public string? CurrentTimeSlot { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public List<RescheduleDayAvailability> AvailableDays { get; set; } = new();
}

public class RescheduleDayAvailability
{
    public DateTime Date { get; set; }
    public List<TimeSlotItem> TimeSlots { get; set; } = new();
}
