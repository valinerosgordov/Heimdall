using Heimdall.Domain.SharedKernel;

namespace Heimdall.Api.Common;

/// <summary>Maps the Result pattern onto HTTP responses (RFC 9457 Problem Details on failure).</summary>
internal static class ResultExtensions
{
    public static IResult ToHttpResult<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess)
        => result.Match(onSuccess, ToProblem);

    public static IResult ToHttpResult(this Result result, Func<IResult> onSuccess)
        => result.IsSuccess ? onSuccess() : ToProblem(result.Error);

    public static IResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(title: error.Code, detail: error.Message, statusCode: statusCode);
    }
}
