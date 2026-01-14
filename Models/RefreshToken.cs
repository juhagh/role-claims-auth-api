using System.ComponentModel.DataAnnotations;

namespace RoleClaimsApp.Models;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string TokenHash { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
}