using FluentValidation.TestHelper;

using Judhur.Application.Features.Users.Dto;
using Judhur.Application.Features.Users.Queries.GetUsers;

using static Judhur.Application.UnitTests.Validators.ValidationData;

namespace Judhur.Application.UnitTests.Validators.Users;

public sealed class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    [Theory]
    [InlineData(AdminUserStatus.All)]
    [InlineData(AdminUserStatus.Active)]
    [InlineData(AdminUserStatus.Banned)]
    [InlineData(AdminUserStatus.Unconfirmed)]
    public void Validate_DefinedStatus_HasNoErrors(AdminUserStatus status)
    {
        _validator.TestValidate(new GetUsersQuery(null, status, 1, 20)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_UndefinedStatus_ReturnsError()
    {
        _validator.TestValidate(new GetUsersQuery(null, (AdminUserStatus)99, 1, 20))
            .ShouldHaveValidationErrorFor(q => q.Status)
            .WithErrorMessage("حالة المستخدم غير صالحة.");
    }

    [Fact]
    public void Validate_SearchAtMaxLength_HasNoError()
    {
        _validator.TestValidate(new GetUsersQuery(Text(100), AdminUserStatus.All, 1, 20)).ShouldNotHaveValidationErrorFor(q => q.Search);
    }

    [Fact]
    public void Validate_SearchOverMaxLength_ReturnsError()
    {
        _validator.TestValidate(new GetUsersQuery(Text(101), AdminUserStatus.All, 1, 20))
            .ShouldHaveValidationErrorFor(q => q.Search)
            .WithErrorMessage("لا يمكن أن يتجاوز نص البحث 100 حرف.");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 1000)]
    public void Validate_PagingIsLeftToHandler_HasNoErrors(int page, int pageSize)
    {
        _validator.TestValidate(new GetUsersQuery(null, AdminUserStatus.All, page, pageSize)).ShouldNotHaveAnyValidationErrors();
    }
}
