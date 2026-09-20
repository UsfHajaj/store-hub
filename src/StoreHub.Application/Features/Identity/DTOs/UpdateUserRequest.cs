namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class UpdateUserRequest
{
    public string UserName { get; set; } = string.Empty;

    public string? NameAr { get; set; }

    public string? NameEn { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? NewPassword { get; set; }
}
