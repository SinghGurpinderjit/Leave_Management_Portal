using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators;

public partial class CreateLeaveRequestDtoValidator
{
    /// <summary>
    /// Validates query params for GET /api/v1/leave-requests/team (Manager only)
    /// </summary>
    public class TeamLeaveRequestQueryValidator : AbstractValidator<TeamLeaveRequestQuery>
    {
        private static readonly string[] AllowedStatuses =
            ["All", "Pending", "Approved", "Rejected", "Cancelled"];

        public TeamLeaveRequestQueryValidator()
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
        }
    }

}
