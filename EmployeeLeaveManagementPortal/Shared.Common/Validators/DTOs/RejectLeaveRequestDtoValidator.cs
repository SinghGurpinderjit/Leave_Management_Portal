using FluentValidation;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators;

public class RejectLeaveRequestDtoValidator : AbstractValidator<RejectLeaveRequestDto>
{
    public RejectLeaveRequestDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required when rejecting a leave request")
            .MinimumLength(10).WithMessage("Rejection reason must be at least 10 characters")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters");
    }
}