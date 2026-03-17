using FluentValidation;
using MediatR;

namespace FinanceTracker.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior — automatically validates every request
/// before it reaches its handler.
/// 
/// HOW IT WORKS: MediatR has a pipeline concept (like ASP.NET middleware).
/// Every request passes through registered behaviors before hitting the handler.
/// This behavior finds all FluentValidation validators for the request type,
/// runs them, and short-circuits with errors if validation fails.
/// 
/// WHY: Without this, every handler would need to manually validate input.
/// With this, validation is automatic and consistent across ALL handlers.
/// Write a validator class → it just works.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
