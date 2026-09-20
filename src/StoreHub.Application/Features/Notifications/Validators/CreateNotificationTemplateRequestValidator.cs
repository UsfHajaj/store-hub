using FluentValidation;
using StoreHub.Application.Features.Notifications.DTOs;

namespace StoreHub.Application.Features.Notifications.Validators;

public sealed class CreateNotificationTemplateRequestValidator : AbstractValidator<CreateNotificationTemplateRequest>
{
    public CreateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SubjectTemplate).MaximumLength(512);
        RuleFor(x => x.BodyTemplate).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.NotificationType).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum();
    }
}
