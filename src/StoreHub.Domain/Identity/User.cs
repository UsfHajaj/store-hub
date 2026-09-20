using StoreHub.Shared.Abstractions;

namespace StoreHub.Domain.Identity;

public sealed class User : AuditableEntity
{
    public string UserName { get; set; } = null!;

    public string? NameAr { get; set; }

    public string? NameEn { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
