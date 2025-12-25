using FluentValidation;
using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Validators;

public class UpdateUserDetailsDtoValidator : AbstractValidator<UpdateUserDetailsDto>
{
    public UpdateUserDetailsDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(64);
        RuleFor(x => x.PhoneNumber).MaximumLength(32).When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.FirstName).MaximumLength(64).When(x => !string.IsNullOrEmpty(x.FirstName));
        RuleFor(x => x.LastName).MaximumLength(64).When(x => !string.IsNullOrEmpty(x.LastName));
        RuleFor(x => x.Avatar).MaximumLength(512).When(x => !string.IsNullOrEmpty(x.Avatar));
    }
}
