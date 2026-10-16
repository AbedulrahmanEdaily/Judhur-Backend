using System.Text.Json;
using Judhur.Domain.Common.Results;

namespace Judhur.Domain.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error First = Error.Validation("Test.First", "الأول");
    private static readonly Error Second = Error.Conflict("Test.Second", "الثاني");

    [Fact]
    public void FromValue_ReturnsSuccessWithValue()
    {
        Result<string> result = "قيمة";

        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal("قيمة", result.Value);
    }

    [Fact]
    public void FromValue_HasNoErrors()
    {
        Result<string> result = "قيمة";

        Assert.Empty(result.Errors);
        Assert.Equal(default, result.TopError);
    }

    [Fact]
    public void FromValue_DefaultValueType_ReturnsSuccess()
    {
        Result<int> result = 0;

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void FromValue_Null_ThrowsArgumentNullException()
    {
        string value = null!;

        Assert.Throws<ArgumentNullException>(() =>
        {
            Result<string> result = value;
            return result;
        });
    }

    [Fact]
    public void FromError_ReturnsErrorWithThatError()
    {
        Result<string> result = First;

        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.Equal(First, Assert.Single(result.Errors));
        Assert.Equal(First, result.TopError);
    }

    [Fact]
    public void FromError_ValueIsDefault()
    {
        Result<string> result = First;

        Assert.Null(result.Value);
    }

    [Fact]
    public void FromErrorList_KeepsAllErrorsInOrder()
    {
        Result<string> result = new List<Error> { First, Second };

        Assert.True(result.IsError);
        Assert.Equal([First, Second], result.Errors);
        Assert.Equal(First, result.TopError);
    }

    [Fact]
    public void FromErrorList_Empty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Result<string> result = new List<Error>();
            return result;
        });
    }

    [Fact]
    public void FromErrorList_Null_ThrowsArgumentException()
    {
        List<Error> errors = null!;

        Assert.Throws<ArgumentException>(() =>
        {
            Result<string> result = errors;
            return result;
        });
    }

    [Fact]
    public void FromOtherResultErrors_PropagatesErrors()
    {
        Result<int> source = new List<Error> { First, Second };

        Result<string> result = source.Errors;

        Assert.Equal([First, Second], result.Errors);
    }

    [Fact]
    public void Match_Success_CallsValueBranch()
    {
        Result<int> result = 21;

        var output = result.Match(value => value * 2, _ => -1);

        Assert.Equal(42, output);
    }

    [Fact]
    public void Match_Error_CallsErrorBranchWithErrors()
    {
        Result<int> result = new List<Error> { First, Second };

        var output = result.Match(_ => "value", errors => string.Join(",", errors.Select(e => e.Code)));

        Assert.Equal("Test.First,Test.Second", output);
    }

    [Fact]
    public void Markers_AreDefaultValues()
    {
        Assert.Equal(default(Success), Result.Success);
        Assert.Equal(default(Created), Result.Created);
        Assert.Equal(default(Deleted), Result.Deleted);
        Assert.Equal(default(Updated), Result.Updated);
    }

    [Fact]
    public void FromMarker_ReturnsSuccess()
    {
        Result<Updated> result = Result.Updated;

        Assert.True(result.IsSuccess);
        Assert.Equal(Result.Updated, result.Value);
    }

    [Fact]
    public void Json_SuccessRoundTrip_KeepsValue()
    {
        Result<string> original = "قيمة";

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Result<string>>(json)!;

        Assert.True(restored.IsSuccess);
        Assert.Equal("قيمة", restored.Value);
        Assert.Empty(restored.Errors);
    }

    [Fact]
    public void Json_SuccessRoundTripOfMarker_KeepsSuccess()
    {
        Result<Updated> original = Result.Updated;

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<Result<Updated>>(json)!;

        Assert.True(restored.IsSuccess);
    }

    [Fact]
    public void Json_SuccessWithNullValue_ThrowsArgumentNullException()
    {
        const string json = """{"Value":null,"Errors":[],"IsSuccess":true}""";

        Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<Result<string>>(json));
    }

    [Fact]
    public void Json_FailureWithoutErrors_ThrowsArgumentException()
    {
        const string json = """{"Value":null,"Errors":[],"IsSuccess":false}""";

        Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<Result<string>>(json));
    }

    [Fact]
    public void Json_FailureWithErrors_ReturnsErrorResult()
    {
        const string json = """{"Value":null,"Errors":[{}],"IsSuccess":false}""";

        var restored = JsonSerializer.Deserialize<Result<string>>(json)!;

        Assert.True(restored.IsError);
        Assert.Single(restored.Errors);
    }
}
