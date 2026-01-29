using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("services")]
public class Service
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("service_name")]
    public string ServiceName { get; set; }
    [Column("description")]
    public string Description { get; set; }
    [Column("category_id")]
    public int? CategoryId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("image")]
    public string? Image { get; set; }
    [Column("icon")]
    public string? Icon { get; set; }
    [Column("base_price")]
    public decimal? BasePrice { get; set; }
    // Navigation properties
    public Category Category { get; set; }
    public ICollection<BookingRequest> BookingRequests { get; set; }
    public ICollection<ProviderService> ProviderServices { get; set; }
    public ICollection<ServicePrice> ServicePrices { get; set; }
}