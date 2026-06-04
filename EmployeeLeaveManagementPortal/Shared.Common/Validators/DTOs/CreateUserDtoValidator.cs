using FluentValidation;
using Shared.Common.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Validators.DTOs
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name must not exceed 100 characters.")
                .Matches(@"^[a-zA-Z\s]+$").WithMessage("First name can only contain letters and spaces.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.")
                .Matches(@"^[a-zA-Z0-9\s]+$").WithMessage("Last name can only contain letters, numbers and spaces.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
                .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role is required.")
                .NotEqual(Guid.Empty).WithMessage("A valid role must be selected.");

            RuleFor(x => x.ManagerId)
                .NotEqual(Guid.Empty)
                .WithMessage("Manager ID must not be an empty GUID.")
                .When(x => x.ManagerId.HasValue);
        }
    }
}
