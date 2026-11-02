using FluentValidation;
using FluentValidation.Results;
using HolidayService.Application.Exceptions;
using MediatR;

namespace HolidayService.Application.Behaviors;

/// <summary>
/// Runs every registered validator for a request before the handler sees it, so handlers
/// may assume well-formed input.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        IValidator<TRequest>[] applicable = [.. validators];

        if (applicable.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] results = await Task.WhenAll(
            applicable.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        Dictionary<string, string[]> failures = results
            .SelectMany(static result => result.Errors)
            .Where(static failure => failure is not null)
            .GroupBy(
                static failure => failure.PropertyName,
                static failure => failure.ErrorMessage,
                StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        if (failures.Count > 0)
        {
            throw new ValidationAppException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
