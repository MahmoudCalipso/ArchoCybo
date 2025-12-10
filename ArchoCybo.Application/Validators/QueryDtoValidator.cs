using FluentValidation;
using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Validators;

public class QueryDtoValidator : AbstractValidator<QueryDto>
{
    public QueryDtoValidator()
    {
        RuleFor(x => x.Sql).NotEmpty().MinimumLength(6);
        RuleFor(x => x.TimeoutSeconds).GreaterThan(0).When(x => x.TimeoutSeconds.HasValue);
    }
}
