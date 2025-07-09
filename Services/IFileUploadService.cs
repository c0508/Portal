using Microsoft.AspNetCore.Http;
using ESGPlatform.Models.ViewModels;

namespace ESGPlatform.Services;

public interface IFileUploadService
{
    Task<FileUploadResult> UploadFileAsync(IFormFile file, string userId);
    Task<bool> DeleteFileAsync(int fileId, string userId);
    bool IsValidFileType(IFormFile file);
    bool IsValidFileSize(IFormFile file);
    string GenerateSecureFileName(string originalFileName);
    string GetFileExtension(string fileName);
    bool IsAllowedExtension(string extension);
    bool IsAllowedContentType(string contentType);
} 