using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.UnitTests.TestDoubles;

public sealed record PlainTestCommand(string Name) : IRequest<Result<string>>;

public sealed record TransactionalTestCommand(string Name) : IRequest<Result<string>>, ITransactionalCommand;

public sealed record InvalidatingTestCommand(string[] InvalidatedTags) : IInvalidateCacheCommand<Result<string>>
{
    public string[] Tags => InvalidatedTags;
}

public sealed record CachedTestQuery(string Key, bool Cacheable = true) : ICachedQuery<Result<string>>
{
    public string CacheKey => Key;

    public string[] Tags => ["things", "others"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);

    public bool IsCacheable => Cacheable;
}

public static class TestResults
{
    public static readonly Error Failure = Error.Conflict("Test.Failure", "فشل");

    public static Task<Result<string>> Success(string value = "ok") => Task.FromResult<Result<string>>(value);

    public static Task<Result<string>> Failed() => Task.FromResult<Result<string>>(Failure);
}
