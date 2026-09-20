using FluentValidation;
using StoreHub.Application.Features.Notifications.DTOs;
using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.Notifications.Validators;

public sealed class NotificationFilterRequestValidator : AbstractValidator<NotificationFilterRequest>
{
    public NotificationFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationConstants.MaxPageSize);
    }
}
