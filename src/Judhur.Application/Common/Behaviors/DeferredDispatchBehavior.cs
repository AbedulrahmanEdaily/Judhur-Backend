using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Common.Behaviors;

public sealed class DeferredDispatchBehavior<TRequest, TResponse>(IDeferredDispatcher deferredDispatcher) : IPipelineBehavior<TRequest, TResponse>
where TRequest : IRequest<TResponse>
where TResponse : IResult
{
    private readonly IDeferredDispatcher _deferredDispatcher = deferredDispatcher;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);
        if (response.IsSuccess)
        {
            await _deferredDispatcher.DispatchAsync(CancellationToken.None);
        }
        return response;
    }
}