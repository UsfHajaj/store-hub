using FluentValidation;
using StoreHub.Application.Features.Identity.DTOs;

namespace StoreHub.Application.Features.Identity.Validators;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DescriptionAr).MaximumLength(512);
        RuleFor(x => x.DescriptionEn).MaximumLength(512);
    }
}
