using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators;

/// <summary>
/// Validates PATCH /api/v1/leave-requests/{id}/status body.
/// Merged the old separate ApproveLeaveRequestDto / RejectLeaveRequestDto into one.
/// Status must be Approved or Rejected — Pending and Cancelled are not manager actions.
/// Reason is mandatory when rejecting.
/// </summary>
public class UpdateLeaveRequestStatusDtoValidator : AbstractValidator<UpdateLeaveRequestStatusDto>
{
    private static readonly string[] AllowedStatuses = ["Approved", "Rejected"];

    public UpdateLeaveRequestStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => AllowedStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Status must be either 'Approved' or 'Rejected'. " +
                         "Managers cannot set Pending or Cancelled.");

        //// Reason is mandatory when rejecting
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required when rejecting a leave request.")
            .MinimumLength(10)
            .WithMessage("Rejection reason must be at least 10 characters.")
            .MaximumLength(500)
            .WithMessage("Rejection reason must not exceed 500 characters.")
            .When(x => string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase));

        // Reason optional for Approved — but length-checked if provided
        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters.");
    }
}