using System;

namespace APIServiceManagement.Domain.Entities;

public class Document
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public string DocumentType { get; set; }
    public string FileUrl { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public long? FileSize { get; set; }
    public string FileName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; }
}