using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class Banner
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string BackgroundColor { get; set; }
    public string BackgroundGradient { get; set; }
    public string ActionUrl { get; set; }
    public string ActionText { get; set; } = "Book Now";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int DisplayDurationSeconds { get; set; } = 3;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}