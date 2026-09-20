namespace StoreHub.Application.Features.Identity.DTOs;

public sealed class UpdateRoleRequest
{
    public string NameAr { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? DescriptionAr { get; set; }

    public string? DescriptionEn { get; set; }
}
