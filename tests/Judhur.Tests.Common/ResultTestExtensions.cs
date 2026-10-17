using Judhur.Domain.Common.Results;

namespace Judhur.Tests.Common;

public static class ResultTestExtensions
{
    public static TValue GetValueOrThrow<TValue>(this Result<TValue> result)
    {
        if (result.IsError)
        {
            var codes = string.Join(", ", result.Errors.Select(e => e.Code));
            throw new InvalidOperationException($"Expected a successful result but got: {codes}");
        }

        return result.Value;
    }

    public static void ThrowIfError<TValue>(this Result<TValue> result)
        => result.GetValueOrThrow();
}
