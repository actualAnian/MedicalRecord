namespace Register.App.DTO
{
    public record RegisterDiagnosisPayload(
        bool Success,
        IReadOnlyList<Error>? Errors)
        {
            public static RegisterDiagnosisPayload Created()
                => new(true, default);

            public static RegisterDiagnosisPayload Invalid(
                IReadOnlyList<Error> error)
                => new(false, error);
        }
}
