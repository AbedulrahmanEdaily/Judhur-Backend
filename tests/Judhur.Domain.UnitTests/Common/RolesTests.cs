using Judhur.Domain.Common;

namespace Judhur.Domain.UnitTests.Common;

public sealed class RolesTests
{
    [Fact]
    public void User_HasStableName()
    {
        Assert.Equal("User", Roles.User);
    }

    [Fact]
    public void Admin_HasStableName()
    {
        Assert.Equal("Admin", Roles.Admin);
    }
}
