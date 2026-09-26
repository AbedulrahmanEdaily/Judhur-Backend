using FluentValidation;

namespace Judhur.Application.Features.Properties.Queries.GetPropertyById;

public sealed class GetPropertyByIdQueryValidator : AbstractValidator<GetPropertyByIdQuery>
{
    public GetPropertyByIdQueryValidator()
    {
        RuleFor(p => p.PropertyId)
        .NotEmpty().WithMessage("PropertyId is required");
    }
}