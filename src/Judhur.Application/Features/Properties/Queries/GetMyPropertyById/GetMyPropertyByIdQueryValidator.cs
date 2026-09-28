using FluentValidation;

namespace Judhur.Application.Features.Properties.Queries.GetMyPropertyById;

public sealed class GetMyPropertyByIdQueryValidator : AbstractValidator<GetMyPropertyByIdQuery>
{
    public GetMyPropertyByIdQueryValidator()
    {
        RuleFor(p => p.PropertyId)
            .NotEmpty().WithMessage("معرّف العقار مطلوب.");
    }
}
