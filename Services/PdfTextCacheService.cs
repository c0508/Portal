using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ESGPlatform.Services;

public interface IPdfTextCacheService
{
    Task<string?> GetCachedTextAsync(string fileHash, int organizationId);
    Task CacheTextAsync(string fileHash, string extractedText, int organizationId);
    Task<bool> IsTextCachedAsync(string fileHash, int organizationId);
    string GenerateFileHash(IFormFile file);
}

public class PdfTextCacheService : IPdfTextCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PdfTextCacheService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24); // Cache for 24 hours

    public PdfTextCacheService(IMemoryCache cache, ILogger<PdfTextCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<string?> GetCachedTextAsync(string fileHash, int organizationId)
    {
        var cacheKey = GenerateCacheKey(fileHash, organizationId);
        
        if (_cache.TryGetValue(cacheKey, out string? cachedText))
        {
            _logger.LogInformation("Retrieved cached PDF text for organization {OrganizationId}, file hash: {FileHash}", 
                organizationId, fileHash);
            return Task.FromResult<string?>(cachedText);
        }

        _logger.LogDebug("No cached text found for organization {OrganizationId}, file hash: {FileHash}", 
            organizationId, fileHash);
        return Task.FromResult<string?>(null);
    }

    public Task CacheTextAsync(string fileHash, string extractedText, int organizationId)
    {
        var cacheKey = GenerateCacheKey(fileHash, organizationId);
        
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheExpiration,
            SlidingExpiration = TimeSpan.FromHours(6) // Extend cache if accessed within 6 hours
        };

        _cache.Set(cacheKey, extractedText, cacheOptions);
        
        _logger.LogInformation("Cached PDF text for organization {OrganizationId}, file hash: {FileHash}, text length: {TextLength}", 
            organizationId, fileHash, extractedText.Length);
        
        return Task.CompletedTask;
    }

    public Task<bool> IsTextCachedAsync(string fileHash, int organizationId)
    {
        var cacheKey = GenerateCacheKey(fileHash, organizationId);
        return Task.FromResult(_cache.TryGetValue(cacheKey, out _));
    }

    public string GenerateFileHash(IFormFile file)
    {
        using var sha256 = SHA256.Create();
        using var stream = file.OpenReadStream();
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateCacheKey(string fileHash, int organizationId)
    {
        // Create organization-scoped cache key to ensure data isolation
        return $"pdf_text_{organizationId}_{fileHash}";
    }
} 