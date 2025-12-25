using FluentValidation;
using ArchoCybo.Application.DTOs;

namespace ArchoCybo.Application.Validators;

public class UpdateUserPermissionsDtoValidator : AbstractValidator<UpdateUserPermissionsDto>
{
    public UpdateUserPermissionsDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.AllowedPermissionIds).NotNull();
    }
}
