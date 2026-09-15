using FluentValidation;

using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validator is null)
        {
            return await next(cancellationToken);
        }
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return await next(cancellationToken);
        }
        var errors = validationResult.Errors
            .ConvertAll(failure => Error.Validation(
                code: failure.PropertyName,
                description: failure.ErrorMessage));
        return (dynamic)errors;
    }
}
