using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("ratings")]
public class Rating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("rating_to")]
    public Guid RatingTo { get; set; }

    [Column("rating_by")]
    public Guid RatingBy { get; set; }

    [Column("rating")]
    [Range(1, 5)]
    public int RatingValue { get; set; }

    [Column("review")]
    public string? Review { get; set; }

    [Column("booking_id")]
    public Guid? BookingId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User RatedUser { get; set; }
    public User RatingUser { get; set; }
    public BookingRequest? Booking { get; set; }
}
