using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ESGPlatform.Models.Entities;

public class User : IdentityUser
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public int OrganizationId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();
    public virtual ICollection<Delegation> DelegationsFrom { get; set; } = new List<Delegation>();
    public virtual ICollection<Delegation> DelegationsTo { get; set; } = new List<Delegation>();
    public virtual ICollection<FileUpload> FileUploads { get; set; } = new List<FileUpload>();
    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";
} 