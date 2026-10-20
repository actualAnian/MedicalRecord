using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Register
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
        public sealed record Validation(string Message) : Error("ValidationError", Message);
        public sealed record External(string Message) : Error("ExternalRegistryError", Message);
    }

}
