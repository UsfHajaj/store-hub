using FluentValidation;
using StoreHub.Application.Features.Identity.DTOs;

namespace StoreHub.Application.Features.Identity.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
