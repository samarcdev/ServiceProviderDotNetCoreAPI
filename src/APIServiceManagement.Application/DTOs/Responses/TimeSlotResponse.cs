namespace APIServiceManagement.Application.DTOs.Responses;

public class TimeSlotItem
{
    public string Slot { get; set; } = string.Empty; // e.g., "9-12", "12-3", "3-6"
    public string DisplayName { get; set; } = string.Empty; // e.g., "9:00 AM - 12:00 PM"
    public bool IsAvailable { get; set; }
}

public class TimeSlotsResponse
{
    public bool Success { get; set; }
    public DateTime Date { get; set; }
    public List<TimeSlotItem> TimeSlots { get; set; } = new();
    public string? Message { get; set; }
}
