using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ESGPlatform.Data;
using ESGPlatform.Models.Entities;
using ESGPlatform.Models.ViewModels;
using ESGPlatform.Services;

namespace ESGPlatform.Services;

public class FileUploadService : IFileUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<FileUploadService> _logger;

    // Security configuration
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const string UploadsDirectory = "uploads";
    
    // Allowed file extensions and content types
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".csv", ".jpg", ".jpeg", ".png", ".gif", ".bmp",
        ".zip", ".rar", ".7z"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/csv",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "application/zip",
        "application/x-rar-compressed",
        "application/x-7z-compressed"
    };

    public FileUploadService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<FileUploadService> logger)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string userId)
    {
        try
        {
            // Validate file
            var validationResult = ValidateFile(file);
            if (!validationResult.IsValid)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Message = validationResult.ErrorMessage
                };
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, UploadsDirectory);
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate secure filename
            var secureFileName = GenerateSecureFileName(file.FileName);
            var filePath = Path.Combine(uploadsPath, secureFileName);

            // Check for path traversal attempts
            var normalizedPath = Path.GetFullPath(filePath);
            var normalizedUploadsPath = Path.GetFullPath(uploadsPath);
            
            if (!normalizedPath.StartsWith(normalizedUploadsPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {OriginalPath}", filePath);
                return new FileUploadResult
                {
                    Success = false,
                    Message = "Invalid file path"
                };
            }

            // Save file with proper error handling
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            // Verify file was actually written and is accessible
            if (!File.Exists(filePath))
            {
                return new FileUploadResult
                {
                    Success = false,
                    Message = "Failed to save file"
                };
            }

            // Create file upload record
            var fileUpload = new FileUpload
            {
                FileName = Path.GetFileName(file.FileName), // Store original filename
                FilePath = $"/{UploadsDirectory}/{secureFileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,
                UploadedById = userId
            };

            _context.FileUploads.Add(fileUpload);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File uploaded successfully: {FileName}, Size: {FileSize}, User: {UserId}", 
                fileUpload.FileName, fileUpload.FileSize, userId);

            return new FileUploadResult
            {
                Success = true,
                Message = "File uploaded successfully",
                FileId = fileUpload.Id,
                FileName = fileUpload.FileName,
                FilePath = fileUpload.FilePath,
                FileSize = fileUpload.FileSize,
                ContentType = fileUpload.ContentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file: {FileName}, User: {UserId}", file.FileName, userId);
            return new FileUploadResult
            {
                Success = false,
                Message = "Error uploading file"
            };
        }
    }

    public async Task<bool> DeleteFileAsync(int fileId, string userId)
    {
        try
        {
            var fileUpload = await _context.FileUploads
                .FirstOrDefaultAsync(f => f.Id == fileId);

            if (fileUpload == null)
            {
                return false;
            }

            // Verify user has permission to delete this file
            if (fileUpload.UploadedById != userId)
            {
                _logger.LogWarning("Unauthorized file deletion attempt: FileId {FileId}, User {UserId}", fileId, userId);
                return false;
            }

            // Delete physical file
            var fileName = Path.GetFileName(fileUpload.FilePath);
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, UploadsDirectory, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Delete database record
            _context.FileUploads.Remove(fileUpload);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File deleted successfully: {FileName}, User: {UserId}", fileUpload.FileName, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: FileId {FileId}, User: {UserId}", fileId, userId);
            return false;
        }
    }

    public bool IsValidFileType(IFormFile file)
    {
        if (file == null || string.IsNullOrEmpty(file.FileName))
            return false;

        var extension = GetFileExtension(file.FileName);
        var contentType = file.ContentType;

        return IsAllowedExtension(extension) && IsAllowedContentType(contentType);
    }

    public bool IsValidFileSize(IFormFile file)
    {
        return file != null && file.Length > 0 && file.Length <= MaxFileSizeBytes;
    }

    public string GenerateSecureFileName(string originalFileName)
    {
        if (string.IsNullOrEmpty(originalFileName))
            return $"{Guid.NewGuid()}.bin";

        var extension = GetFileExtension(originalFileName);
        var secureName = $"{Guid.NewGuid()}{extension}";
        
        return secureName;
    }

    public string GetFileExtension(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    public bool IsAllowedExtension(string extension)
    {
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension.ToLowerInvariant());
    }

    public bool IsAllowedContentType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType) && AllowedContentTypes.Contains(contentType.ToLowerInvariant());
    }

    private FileUploadValidationResult ValidateFile(IFormFile file)
    {
        if (file == null)
        {
            return new FileUploadValidationResult
            {
                IsValid = false,
                ErrorMessage = "No file provided"
            };
        }

        if (file.Length == 0)
        {
            return new FileUploadValidationResult
            {
                IsValid = false,
                ErrorMessage = "File is empty"
            };
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return new FileUploadValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB"
            };
        }

        if (string.IsNullOrEmpty(file.FileName))
        {
            return new FileUploadValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid filename"
            };
        }

        // Check for path traversal in filename
        if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains("\\"))
        {
            return new FileUploadValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid filename"
            };
        }

        if (!IsValidFileType(file))
        {
            return new FileUploadValidationResult
            {
                IsValid = false,
                ErrorMessage = "File type not allowed"
            };
        }

        return new FileUploadValidationResult
        {
            IsValid = true
        };
    }
} 