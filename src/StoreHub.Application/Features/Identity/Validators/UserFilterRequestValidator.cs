using FluentValidation;
using StoreHub.Application.Features.Identity.DTOs;
using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Identity.Validators;

public sealed class UserFilterRequestValidator : AbstractValidator<UserFilterRequest>
{
    public UserFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationConstants.MaxPageSize);
    }
}
