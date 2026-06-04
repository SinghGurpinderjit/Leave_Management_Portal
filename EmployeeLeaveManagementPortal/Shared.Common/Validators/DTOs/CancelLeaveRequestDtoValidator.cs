using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators.DTOs
{

    /// <summary>
    /// Validates PATCH /api/v1/leave-requests/{id}/cancel body.
    /// </summary>
    public class CancelLeaveRequestDtoValidator : AbstractValidator<CancelLeaveRequestDto>
    {
        public CancelLeaveRequestDtoValidator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required when cancelling a leave request.")
                .MinimumLength(10)
                .WithMessage("Cancellation reason must be at least 10 characters.")
                .MaximumLength(500)
                .WithMessage("Reason must not exceed 500 characters.");
        }
    }
}
