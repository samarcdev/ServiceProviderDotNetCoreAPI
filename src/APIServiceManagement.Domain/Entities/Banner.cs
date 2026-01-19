using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("banners")]
public class Banner
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("title")]
    public string Title { get; set; }
    [Column("subtitle")]
    public string Subtitle { get; set; }
    [Column("description")]
    public string Description { get; set; }
    [Column("image_url")]
    public string ImageUrl { get; set; }
    [Column("background_color")]
    public string BackgroundColor { get; set; }
    [Column("background_gradient")]
    public string BackgroundGradient { get; set; }
    [Column("action_url")]
    public string ActionUrl { get; set; }
    [Column("action_text")]
    public string ActionText { get; set; } = "Book Now";
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("display_duration_seconds")]
    public int DisplayDurationSeconds { get; set; } = 3;
    [Column("start_date")]
    public DateTime? StartDate { get; set; }
    [Column("end_date")]
    public DateTime? EndDate { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}