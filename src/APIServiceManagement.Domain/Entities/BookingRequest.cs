using APIServiceManagement.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("booking_requests")]
public class BookingRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("customer_id")]
    public Guid CustomerId { get; set; }
    [Column("service_id")]
    public int ServiceId { get; set; }
    [Column("pincode")]
    public string Pincode { get; set; } = string.Empty;
    [Column("service_provider_id")]
    public Guid? ServiceProviderId { get; set; }
    [Column("admin_id")]
    public Guid? AdminId { get; set; }
    [Column("status_id")]
    public int StatusId { get; set; }
    [Column("status")]
    public string Status { get; set; } = BookingStatusStrings.Pending; // Kept for backward compatibility during migration
    [Column("service_type_id")]
    public int? ServiceTypeId { get; set; }
    [Column("request_description")]
    public string RequestDescription { get; set; } = string.Empty;
    [Column("customer_address")]
    public string CustomerAddress { get; set; } = string.Empty;
    [Column("address_line1")]
    public string? AddressLine1 { get; set; }
    [Column("address_line2")]
    public string? AddressLine2 { get; set; }
    [Column("city")]
    public string? City { get; set; }
    [Column("state")]
    public string? State { get; set; }
    [Column("customer_phone")]
    public string CustomerPhone { get; set; } = string.Empty;
    [Column("alternative_mobile_number")]
    public string? AlternativeMobileNumber { get; set; }
    [Column("customer_name")]
    public string CustomerName { get; set; } = string.Empty; // Kept for backward compatibility, not used in new bookings
    [Column("preferred_date")]
    public DateTime? PreferredDate { get; set; }
    [Column("preferred_time")]
    public TimeSpan? PreferredTime { get; set; }
    [Column("time_slot")]
    public string? TimeSlot { get; set; } // e.g., "9-12", "12-3", "3-6"
    [Column("estimated_price")]
    public decimal? EstimatedPrice { get; set; }
    [Column("final_price")]
    public decimal? FinalPrice { get; set; }
    [Column("base_price")]
    public decimal? BasePrice { get; set; }
    [Column("location_adjustment_amount")]
    public decimal? LocationAdjustmentAmount { get; set; }
    [Column("discount_id")]
    public int? DiscountId { get; set; }
    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }
    [Column("cgst_amount")]
    public decimal? CgstAmount { get; set; }
    [Column("sgst_amount")]
    public decimal? SgstAmount { get; set; }
    [Column("igst_amount")]
    public decimal? IgstAmount { get; set; }
    [Column("service_charge_amount")]
    public decimal? ServiceChargeAmount { get; set; }
    [Column("platform_charge_amount")]
    public decimal? PlatformChargeAmount { get; set; }
    [Column("total_tax_amount")]
    public decimal? TotalTaxAmount { get; set; }
    [Column("admin_notes")]
    public string AdminNotes { get; set; } = string.Empty;
    [Column("service_provider_notes")]
    public string ServiceProviderNotes { get; set; } = string.Empty;
    [Column("customer_rating")]
    public int? CustomerRating { get; set; }
    [Column("customer_feedback")]
    public string CustomerFeedback { get; set; } = string.Empty;
    [Column("assigned_at")]
    public DateTime? AssignedAt { get; set; }
    [Column("started_at")]
    public DateTime? StartedAt { get; set; }
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("working_hours")]
    public int WorkingHours { get; set; } = 1; 
    // Navigation properties
    public User Customer { get; set; }
    public Service Service { get; set; }
    public ServiceType? ServiceType { get; set; }
    public User ServiceProvider { get; set; }
    public User Admin { get; set; }
    public BookingStatus? StatusNavigation { get; set; }
    public ICollection<BookingStatusHistory> BookingStatusHistories { get; set; }
    public DiscountMaster? Discount { get; set; }
}