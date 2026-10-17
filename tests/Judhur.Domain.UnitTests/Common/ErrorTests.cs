using Judhur.Domain.Common.Results;

namespace Judhur.Domain.UnitTests.Common;

public sealed class ErrorTests
{
    private static Error Make(ErrorKind kind, string code, string description) => kind switch
    {
        ErrorKind.Failure => Error.Failure(code, description),
        ErrorKind.Unexpected => Error.Unexpected(code, description),
        ErrorKind.Validation => Error.Validation(code, description),
        ErrorKind.Conflict => Error.Conflict(code, description),
        ErrorKind.NotFound => Error.NotFound(code, description),
        ErrorKind.Unauthorized => Error.Unauthorized(code, description),
        ErrorKind.Forbidden => Error.Forbidden(code, description),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static Error MakeWithDefaults(ErrorKind kind) => kind switch
    {
        ErrorKind.Failure => Error.Failure(),
        ErrorKind.Unexpected => Error.Unexpected(),
        ErrorKind.Validation => Error.Validation(),
        ErrorKind.Conflict => Error.Conflict(),
        ErrorKind.NotFound => Error.NotFound(),
        ErrorKind.Unauthorized => Error.Unauthorized(),
        ErrorKind.Forbidden => Error.Forbidden(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    [Theory]
    [InlineData(ErrorKind.Failure)]
    [InlineData(ErrorKind.Unexpected)]
    [InlineData(ErrorKind.Validation)]
    [InlineData(ErrorKind.Conflict)]
    [InlineData(ErrorKind.NotFound)]
    [InlineData(ErrorKind.Unauthorized)]
    [InlineData(ErrorKind.Forbidden)]
    public void Factory_GivenCodeAndDescription_SetsAllFields(ErrorKind kind)
    {
        var error = Make(kind, "Test.Code", "وصف الخطأ");

        Assert.Equal("Test.Code", error.Code);
        Assert.Equal("وصف الخطأ", error.Description);
        Assert.Equal(kind, error.Type);
    }

    [Theory]
    [InlineData(ErrorKind.Failure)]
    [InlineData(ErrorKind.Unexpected)]
    [InlineData(ErrorKind.Validation)]
    [InlineData(ErrorKind.Conflict)]
    [InlineData(ErrorKind.NotFound)]
    [InlineData(ErrorKind.Unauthorized)]
    [InlineData(ErrorKind.Forbidden)]
    public void Factory_NoArguments_UsesKindNameAsCodeAndDefaultDescription(ErrorKind kind)
    {
        var error = MakeWithDefaults(kind);

        Assert.Equal(kind.ToString(), error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Description));
        Assert.Equal(kind, error.Type);
    }

    [Theory]
    [InlineData(ErrorKind.Failure)]
    [InlineData(ErrorKind.Unexpected)]
    [InlineData(ErrorKind.Validation)]
    [InlineData(ErrorKind.Conflict)]
    [InlineData(ErrorKind.NotFound)]
    [InlineData(ErrorKind.Unauthorized)]
    [InlineData(ErrorKind.Forbidden)]
    public void Create_KindAsInteger_MapsToSameKind(ErrorKind kind)
    {
        var error = Error.Create((int)kind, "Test.Code", "وصف");

        Assert.Equal(kind, error.Type);
        Assert.Equal("Test.Code", error.Code);
        Assert.Equal("وصف", error.Description);
    }

    [Fact]
    public void Create_MatchingFactory_IsEqual()
    {
        var created = Error.Create((int)ErrorKind.Conflict, "Test.Code", "وصف");

        Assert.Equal(Error.Conflict("Test.Code", "وصف"), created);
    }

    [Fact]
    public void Equals_SameCodeDescriptionAndKind_ReturnsTrue()
    {
        var first = Error.Validation("Test.Code", "وصف");
        var second = Error.Validation("Test.Code", "وصف");

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentKind_ReturnsFalse()
    {
        var validation = Error.Validation("Test.Code", "وصف");
        var conflict = Error.Conflict("Test.Code", "وصف");

        Assert.NotEqual(validation, conflict);
    }

    [Fact]
    public void Equals_DifferentCode_ReturnsFalse()
    {
        Assert.NotEqual(Error.NotFound("A", "وصف"), Error.NotFound("B", "وصف"));
    }

    [Fact]
    public void Default_HasNoCodeOrDescription()
    {
        var error = default(Error);

        Assert.Null(error.Code);
        Assert.Null(error.Description);
        Assert.Equal(ErrorKind.Failure, error.Type);
    }
}
