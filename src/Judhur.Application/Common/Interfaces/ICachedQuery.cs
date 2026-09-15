using MediatR;

namespace Judhur.Application.Common.Interfaces;

public interface ICachedQuery
{
    string CacheKey { get; }

    string[] Tags { get; }

    TimeSpan Expiration { get; }
}

public interface ICachedQuery<TResponse> : IRequest<TResponse>, ICachedQuery;