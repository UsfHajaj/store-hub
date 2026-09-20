namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? NameEn { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public IReadOnlyList<Guid> RoleIds { get; set; } = Array.Empty<Guid>();
}
