using Microsoft.AspNetCore.Identity;

namespace Judhur.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? ProfileImageUrl { get; set; }
    public string FullName { get; set; } = null!;
    public string? ResetCode { get; set; }
    public DateTimeOffset? ResetCodeExpiresAt { get; set; }
    public int ResetCodeFailedAttempts { get; set; }
    public string City { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfileImagePublicId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
};