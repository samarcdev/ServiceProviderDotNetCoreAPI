using System;
using System.Collections.Generic;

namespace APIServiceManagement.Domain.Entities;

public class Service
{
    public int Id { get; set; }
    public string ServiceName { get; set; }
    public string Description { get; set; }
    public int? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Image { get; set; }
    public string Icon { get; set; }

    // Navigation properties
    public Category Category { get; set; }
    public ICollection<BookingRequest> BookingRequests { get; set; }
    public ICollection<ProviderService> ProviderServices { get; set; }
    public ICollection<ServiceAvailablePincode> ServiceAvailablePincodes { get; set; }
    public ICollection<ServicePrice> ServicePrices { get; set; }
}