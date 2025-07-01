using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public class UserContext
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)] // IdentityUser Id length
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int OrganizationId { get; set; }

    // When a supplier user is viewing campaigns from a specific platform organization
    public int? ActivePlatformOrganizationId { get; set; }

    public DateTime LastSwitched { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(ActivePlatformOrganizationId))]
    public virtual Organization? ActivePlatformOrganization { get; set; }

    // Computed properties
    [NotMapped]
    public bool HasActiveContext => ActivePlatformOrganizationId.HasValue;

    [NotMapped]
    public string ContextDisplayName => HasActiveContext 
        ? $"Viewing campaigns from {ActivePlatformOrganization?.Name}"
        : "All campaigns";
} 