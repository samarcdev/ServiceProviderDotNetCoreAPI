using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("admin_state_assignments")]
public class AdminStateAssignment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("admin_user_id")]
    public Guid AdminUserId { get; set; }
    [Column("state_id")]
    public int StateId { get; set; } 
    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User AdminUser { get; set; }
    public State State { get; set; }
    public User AssignedByUser { get; set; }
}