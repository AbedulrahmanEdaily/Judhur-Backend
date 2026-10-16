using Judhur.Domain.Common.Results;

namespace Judhur.Application.UnitTests.TestDoubles;

public static class ResultTask
{
    public static Task<Result<T>> Of<T>(T value) => Task.FromResult<Result<T>>(value);

    public static Task<Result<T>> Failed<T>(Error error) => Task.FromResult<Result<T>>(error);
}
