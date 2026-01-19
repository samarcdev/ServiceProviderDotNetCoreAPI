using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("booking_statuses")]
public class BookingStatus
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("code")]
    public string Code { get; set; } = string.Empty;
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("description")]
    public string? Description { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
    public ICollection<BookingStatusHistory> BookingStatusHistories { get; set; } = new List<BookingStatusHistory>();
}
