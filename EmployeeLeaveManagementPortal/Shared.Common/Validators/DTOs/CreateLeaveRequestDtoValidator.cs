using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators;

public partial class CreateLeaveRequestDtoValidator : AbstractValidator<CreateLeaveRequestDto>
{
    private static readonly string[] AllowedLeaveTypes = ["All", "Pending", "Approved", "Rejected", "Cancelled"];


    /// <summary>
    /// Validates POST /api/v1/leave-requests body.
    /// Covers all 4 backend validation rules from the assignment:
    ///   1. No past start dates
    ///   2. StartDate <= EndDate
    ///   3. NumberOfDays matches date range
    ///   4. Balance and overlap checks are done in the service layer (not here)
    /// </summary>
    public CreateLeaveRequestDtoValidator()
    {
        RuleFor(x => x.LeaveTypeId)
            .GreaterThan(0)
            .WithMessage("LeaveTypeId must be a valid positive number");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Start date cannot be in the past");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date");

        RuleFor(x => x.NumberOfDays)
            .GreaterThan(0)
            .WithMessage("Number of days must be greater than 0")
            .Must((dto, days) =>
                days == dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1
            )
            .WithMessage("Number of days must match the difference between start and end date");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters")
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters");

        RuleFor(x => x.ReportingManagerId)
            .NotEqual(Guid.Empty)
            .WithMessage("ReportingManagerId must not be an empty GUID")
            .When(x => x.ReportingManagerId.HasValue);
    }

}