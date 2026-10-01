using FluentValidation;

namespace Judhur.Application.Features.Sellers.Queries.GetSellerProfile;

public sealed class GetSellerProfileQueryValidator : AbstractValidator<GetSellerProfileQuery>
{
    public GetSellerProfileQueryValidator()
    {
        RuleFor(q => q.SellerId)
            .NotEmpty().WithMessage("معرّف البائع مطلوب.");
    }
}
