namespace Register.App.DTO
{
    public record ChangeDiagnosisStatusPayload(
        bool Success,
        IReadOnlyList<Error>? Errors,
        Diagnosis? Diagnosis)
    {
        public static ChangeDiagnosisStatusPayload Updated(Diagnosis diagnosis)
            => new(true, default, diagnosis);
        public static ChangeDiagnosisStatusPayload Invalid(
            IReadOnlyList<Error> error)
            => new(false, error, default);
    }
}
