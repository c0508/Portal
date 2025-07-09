namespace ESGPlatform.Models.ViewModels;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? FileId { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
}

public class FileUploadValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
} 