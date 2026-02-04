using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredRoot = configuration["FileStorage:RootPath"];
        _rootPath = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(environment.ContentRootPath, "uploads")
            : configuredRoot;
    }

    public async Task<FileStorageResult> SaveAsync(FileUploadRequest file, string subfolder, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("File is empty.");
        }

        var safeFileName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var targetFolder = Path.Combine(_rootPath, subfolder);

        Directory.CreateDirectory(targetFolder);

        var fullPath = Path.Combine(targetFolder, storedFileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await using var input = file.OpenReadStream();
        await input.CopyToAsync(stream, cancellationToken);

        var relativePath = Path.Combine(subfolder, storedFileName).Replace("\\", "/");

        return new FileStorageResult
        {
            RelativePath = relativePath,
            OriginalFileName = safeFileName,
            Size = file.Length,
            ContentType = file.ContentType ?? string.Empty
        };
    }

    public async Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string subfolder, CancellationToken cancellationToken = default)
    {
        if (fileBytes == null || fileBytes.Length == 0)
        {
            throw new InvalidOperationException("File bytes are empty.");
        }

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var targetFolder = Path.Combine(_rootPath, subfolder);

        Directory.CreateDirectory(targetFolder);

        var fullPath = Path.Combine(targetFolder, storedFileName);
        await File.WriteAllBytesAsync(fullPath, fileBytes, cancellationToken);

        var relativePath = Path.Combine(subfolder, storedFileName).Replace("\\", "/");
        return relativePath;
    }

    public Task<byte[]> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path cannot be empty.", nameof(relativePath));
        }

        var fullPath = Path.Combine(_rootPath, relativePath.Replace("/", "\\"));
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found.", fullPath);
        }

        return File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.FromResult(false);
        }

        try
        {
            var fullPath = Path.Combine(_rootPath, relativePath.Replace("/", "\\"));
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
