using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class DocumentUploadResponse
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}
