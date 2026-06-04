using FluentValidation;
using Shared.Common.DTOs;

namespace Shared.Common.Extensions
{
    public static class FluentValidationExtensions
    {
        public static async Task<ApiResponse<dynamic>?> ValidateRequestAsync<TDto>(
            this IValidator<TDto> validator, TDto dto)
        {
            var validation = await validator.ValidateAsync(dto);
            if (validation.IsValid)
            {
                return null;
            }

            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();

            return ApiResponse<dynamic>.BadRequest(errors);
        }
    }
}
