using FluentValidation;
using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Validators;

public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.DatabaseType).IsInEnum();
        RuleFor(x => x.DatabaseConnectionJson).NotEmpty().When(x => x.DatabaseType != ArchoCybo.Domain.Enums.DatabaseType.SQLite);
    }
}
