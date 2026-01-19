using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("user_registration_steps")]
public class UserRegistrationStep
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("step_number")]
    public int StepNumber { get; set; }
    [Column("step_name")]
    public string StepName { get; set; }
    [Column("step_data")]
    public string StepData { get; set; } = "{}";
    [Column("is_completed")]
    public bool IsCompleted { get; set; } = false;
    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

    // Navigation property
    public User User { get; set; }
}