using FluentValidation;

using Judhur.Domain.Properties;

namespace Judhur.Application.Features.Properties.Queries.GetProperties;

public sealed class GetPropertiesQueryValidator : AbstractValidator<GetPropertiesQuery>
{
    public const int MaxSearchTermLength = 100;

    public GetPropertiesQueryValidator()
    {
        RuleFor(q => q.MinPrice)
            .InclusiveBetween(0, Property.MaxPrice).WithMessage("الحد الأدنى للسعر غير صالح.")
            .When(q => q.MinPrice.HasValue);

        RuleFor(q => q.MaxPrice)
            .InclusiveBetween(0, Property.MaxPrice).WithMessage("الحد الأعلى للسعر غير صالح.")
            .When(q => q.MaxPrice.HasValue);

        RuleFor(q => q.MaxPrice)
            .GreaterThanOrEqualTo(q => q.MinPrice).WithMessage("الحد الأعلى للسعر يجب أن يكون أكبر من الحد الأدنى أو مساويًا له.")
            .When(q => q.MinPrice.HasValue && q.MaxPrice.HasValue);

        RuleFor(q => q.SearchTerm)
            .MaximumLength(MaxSearchTermLength).WithMessage($"لا يمكن أن يتجاوز نص البحث {MaxSearchTermLength} حرف.");
    }
}
