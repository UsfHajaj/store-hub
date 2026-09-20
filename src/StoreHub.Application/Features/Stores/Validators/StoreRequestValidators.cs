using FluentValidation;
using StoreHub.Application.Features.Stores.DTOs;
using StoreHub.Domain.Enums;

namespace StoreHub.Application.Features.Stores.Validators;

public sealed class CreateStoreRequestValidator : AbstractValidator<CreateStoreRequest>
{
    public CreateStoreRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DescriptionAr).MaximumLength(512);
        RuleFor(x => x.DescriptionEn).MaximumLength(512);
        RuleFor(x => x.StoreType).IsInEnum();
    }
}

public sealed class UpdateStoreRequestValidator : AbstractValidator<UpdateStoreRequest>
{
    public UpdateStoreRequestValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DescriptionAr).MaximumLength(512);
        RuleFor(x => x.DescriptionEn).MaximumLength(512);
        RuleFor(x => x.StoreType).IsInEnum();
    }
}
