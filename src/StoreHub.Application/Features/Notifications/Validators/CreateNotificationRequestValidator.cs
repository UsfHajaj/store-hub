using FluentValidation;
using StoreHub.Application.Features.Notifications.DTOs;

namespace StoreHub.Application.Features.Notifications.Validators;

public sealed class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.NotificationType).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.RelatedEntityType).MaximumLength(128);
    }
}
