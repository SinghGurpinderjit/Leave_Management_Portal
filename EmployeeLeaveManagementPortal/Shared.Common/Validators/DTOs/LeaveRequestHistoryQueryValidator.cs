using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators.DTOs
{
    /// <summary>
    /// Validates query params for GET /api/v1/leave-requests/history
    /// </summary>
    public class LeaveRequestHistoryQueryValidator : AbstractValidator<LeaveRequestHistoryQuery>
    {
        private static readonly string[] AllowedStatuses =
            ["All", "Pending", "Approved", "Rejected", "Cancelled"];

        public LeaveRequestHistoryQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be 1 or greater.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.Status)
                .Must(s => s == null ||
                           AllowedStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

            RuleFor(x => x.ToDate)
                .GreaterThanOrEqualTo(x => x.FromDate)
                .WithMessage("ToDate must be on or after FromDate.")
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);

            RuleFor(x => x.ToDate)
                .Must(date => DateOnly.TryParse(date.ToString(), out _))
                .WithMessage("FromDate must be a valid date in format yyyy-MM-dd.")
                .When(x => x.ToDate.HasValue);

            RuleFor(x => x.FromDate)
                .Must(date => DateOnly.TryParse(date.ToString(), out _))
                .WithMessage("FromDate must be a valid date in format yyyy-MM-dd.")
                .When(x => x.FromDate.HasValue);
        }
    }
}
