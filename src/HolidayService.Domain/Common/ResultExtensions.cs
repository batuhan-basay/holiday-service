namespace HolidayService.Domain.Common;

public static class ResultExtensions
{
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);

        return result.IsSuccess
            ? Result.Success(map(result.Value))
            : Result.Failure<TOut>(result.Error);
    }

    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(bind);

        return result.IsSuccess ? bind(result.Value) : Result.Failure<TOut>(result.Error);
    }

    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }
}
