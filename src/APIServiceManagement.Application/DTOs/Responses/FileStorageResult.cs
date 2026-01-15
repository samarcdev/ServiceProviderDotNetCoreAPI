using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class FileStorageResult
{
    public string RelativePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
