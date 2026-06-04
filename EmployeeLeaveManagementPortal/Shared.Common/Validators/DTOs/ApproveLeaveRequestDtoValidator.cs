using FluentValidation;
using Shared.Common.DTOs.Leaves;

namespace Shared.Common.Validators;

public partial class CreateLeaveRequestDtoValidator
{
    public class ApproveLeaveRequestDtoValidator : AbstractValidator<ApproveLeaveRequestDto>
    {
        // No fields to validate — kept for consistency and future extensibility
    }
}
