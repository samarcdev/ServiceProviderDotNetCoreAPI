using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("user_terminations")]
public class UserTermination
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("terminated_by")]
    public Guid TerminatedBy { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = string.Empty;

    [Column("terminated_at")]
    public DateTime TerminatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public User? TerminatedByUser { get; set; }
}
