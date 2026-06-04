using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators;

public partial class CreateLeaveRequestDtoValidator
{
    public class LeaveBalanceDtoValidator : AbstractValidator<LeaveBalanceDto>
    {
        public LeaveBalanceDtoValidator()
        {
            RuleFor(x => x.LeaveType)
                .NotEmpty().WithMessage("Leave type is required")
                .MinimumLength(2).WithMessage("Leave type must be at least 2 characters")
                .MaximumLength(100).WithMessage("Leave type must not exceed 100 characters")
                .Matches(@"^[a-zA-Z\s\-]+$").WithMessage("Leave type can only contain letters, spaces, and hyphens");

            RuleFor(x => x.TotalAllocated)
                .GreaterThan(0).WithMessage("Total allocated days must be greater than 0")
                .LessThanOrEqualTo(365).WithMessage("Total allocated days cannot exceed 365");

            RuleFor(x => x.UsedLeaves)
                .GreaterThanOrEqualTo(0).WithMessage("Used leaves cannot be negative")
                .LessThanOrEqualTo(x => x.TotalAllocated).WithMessage("Used leaves cannot exceed total allocated days");

            RuleFor(x => x.RemainingLeaves)
                .GreaterThanOrEqualTo(0).WithMessage("Remaining leaves cannot be negative")
                .Must((dto, remaining) => remaining == dto.TotalAllocated - dto.UsedLeaves)
                .WithMessage("Remaining leaves must equal total allocated minus used leaves");
        }
    }
}
