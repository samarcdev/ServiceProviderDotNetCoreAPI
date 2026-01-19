using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("documents")]
public class Document
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("user_id")]
    public Guid? UserId { get; set; }
    [Column("document_type")]
    public string DocumentType { get; set; }
    [Column("file_url")]
    public string FileUrl { get; set; }
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    [Column("file_size")]
    public long? FileSize { get; set; }
    [Column("file_name")]
    public string FileName { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; }
}