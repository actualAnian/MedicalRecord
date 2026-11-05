using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using static Register.App.Error;

namespace Register.App
{
    public readonly record struct Result<T>(T? Value, IReadOnlyList<Error> Errors)
    {
        public static Result<T> Success(T value) => new(value, []);
        public static Result<T> Fail(IReadOnlyList<Error> errors) => new(default, errors);

        [MemberNotNullWhen(true, nameof(Value))]
        public bool IsSuccess => Errors.Count == 0;
        [MemberNotNullWhen(true, nameof(Errors))]
        public bool IsFailure => Errors.Count > 0;
        public T GetValueOrThrow()
        {
            if (IsFailure)
                throw new InvalidOperationException(
                    "Cannot get the value of a failed Result.");

            return Value!;
        }
    }
    public abstract record Error(string Code, string Message)
    {
        public sealed record NotFound(string EntityName, Guid Id) : Error("NotFound", $"{EntityName} with ID {Id} was not found.");
        public sealed record NotFoundOnLookup(string EntityName, string Message) : Error("NotFoundOnLookup", $"{EntityName} not found. Message: {Message}");
        public sealed record Validation(string Message) : Error("ValidationError", Message);
        public sealed record External(string Message) : Error("ExternalRegistryError", Message);
        public sealed record IllegalOperation(string Message) : Error("IllegalOperation", Message);
    }
    public static class ResultHttpMapper
    {
        public static IResult ToHttpResult<T>(
            this Result<T> result)
        {
            if (result.IsSuccess)
                return Results.Ok(result.Value);

            var errors = result.Errors;

            var statusCode = errors.Any(e =>
                e is IllegalOperation)
                    ? StatusCodes.Status409Conflict
                : errors.Any(e =>
                    e is NotFound)
                    ? StatusCodes.Status404NotFound
                : errors.Any(e =>
                    e is External)
                    ? StatusCodes.Status502BadGateway
                : errors.Any(e =>
                    e is NotFound || e is NotFoundOnLookup)
                    ? StatusCodes.Status403Forbidden
                : errors.All(e =>
                    e is Validation)
                    ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError;

            return Results.Problem(
                statusCode: statusCode,
                title: "Request failed",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = errors.Select(e => new
                    {
                        e.Code,
                        e.Message
                    })
                });
        }
    }
}
