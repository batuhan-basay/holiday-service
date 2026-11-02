using FluentValidation;
using FluentValidation.Results;
using HolidayService.Application.Abstractions.Messaging;
using HolidayService.Application.Exceptions;
using HolidayService.Domain.Common;

namespace HolidayService.Application.Behaviors;

internal sealed class ValidationBehavior<TQuery, TResponse>(
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryBehavior<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(
        TQuery query,
        QueryHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        IValidator<TQuery>[] applicable = [.. validators];

        if (applicable.Length == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TQuery>(query);

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
