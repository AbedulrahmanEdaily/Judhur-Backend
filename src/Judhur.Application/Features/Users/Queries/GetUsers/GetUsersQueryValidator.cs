using FluentValidation;

namespace Judhur.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(q => q.Status)
            .IsInEnum().WithMessage("حالة المستخدم غير صالحة.");

        RuleFor(q => q.Search)
            .MaximumLength(100).WithMessage("لا يمكن أن يتجاوز نص البحث 100 حرف.");
    }
}
