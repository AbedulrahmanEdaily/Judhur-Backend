using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalCommand
    where TResponse : IResult
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var response = await next(cancellationToken);
        if (!response.IsSuccess)
        {
            await transaction.RollbackAsync(cancellationToken);
            return response;
        }
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
