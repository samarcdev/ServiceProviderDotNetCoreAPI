using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("category_name")]
    public string CategoryName { get; set; }
    [Column("parent_id")]
    public int? ParentId { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("image")]
    public string? Image { get; set; }
    [Column("icon")]
    public string? Icon { get; set; }

    // Navigation properties
    public Category Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; }
    public ICollection<Service> Services { get; set; }
}