using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class Category
{
    [Key]
    public int Id { get; set; }
    public string CategoryName { get; set; }
    public int? ParentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string Image { get; set; }
    public string Icon { get; set; }

    // Navigation properties
    public Category Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; }
    public ICollection<Service> Services { get; set; }
}