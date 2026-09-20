using FluentValidation;
using StoreHub.Application.Features.Notifications.DTOs;

namespace StoreHub.Application.Features.Notifications.Validators;

public sealed class CreateNotificationBatchRequestValidator : AbstractValidator<CreateNotificationBatchRequest>
{
    public CreateNotificationBatchRequestValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty();
        RuleForEach(x => x.UserIds).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.NotificationType).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.RelatedEntityType).MaximumLength(128);
    }
}
