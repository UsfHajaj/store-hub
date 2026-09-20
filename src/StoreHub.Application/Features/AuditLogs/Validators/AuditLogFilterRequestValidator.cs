using FluentValidation;
using StoreHub.Application.Features.AuditLogs.DTOs;
using StoreHub.Shared.Constants;

namespace StoreHub.Application.Features.AuditLogs.Validators;

public sealed class AuditLogFilterRequestValidator : AbstractValidator<AuditLogFilterRequest>
{
    public AuditLogFilterRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationConstants.MaxPageSize);
        RuleFor(x => x.EntityType).MaximumLength(256);
        RuleFor(x => x.Action).MaximumLength(128);
        RuleFor(x => x.Search).MaximumLength(256);

        RuleFor(x => x)
            .Must(x => !x.FromOccurredOnUtc.HasValue || !x.ToOccurredOnUtc.HasValue ||
                       x.ToOccurredOnUtc.Value >= x.FromOccurredOnUtc.Value)
            .WithMessage("ToOccurredOnUtc must be greater than or equal to FromOccurredOnUtc.");
    }
}
