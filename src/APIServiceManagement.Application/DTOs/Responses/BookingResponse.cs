using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class BookingResponse
{
    public bool Success { get; set; }
    public BookingRequestDto? Booking { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string>? Errors { get; set; }
}

public class BookingRequestDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public int ServiceId { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public Guid? ServiceProviderId { get; set; }
    public Guid? AdminId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
    public string? RequestDescription { get; set; }
    public string? CustomerAddress { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? CustomerPhone { get; set; }
    public string? AlternativeMobileNumber { get; set; }
    public string? CustomerName { get; set; } // Kept for backward compatibility
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTime { get; set; }
    public string? TimeSlot { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string? AdminNotes { get; set; }
    public string? ServiceProviderNotes { get; set; }
    public int? CustomerRating { get; set; }
    public string? CustomerFeedback { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int WorkingHours { get; set; }
    public BookingServiceDto? Service { get; set; }
    public BookingUserDto? Customer { get; set; }
    public BookingServiceProviderDto? ServiceProvider { get; set; }
    public BookingAdminDto? Admin { get; set; }
}

public class BookingServiceDto
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
}

public class BookingUserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class BookingServiceProviderDto : BookingUserDto
{
    public decimal? Rating { get; set; }
}

public class BookingAdminDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}
