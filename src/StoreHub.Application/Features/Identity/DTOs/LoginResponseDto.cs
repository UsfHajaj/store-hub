namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public string TokenType { get; init; } = "Bearer";
}
