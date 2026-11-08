namespace Register.App.DTO
{
    public record GetAmountOfDiagnosisWithICDCodePayload(
        bool Success,
        int Amount,
        IReadOnlyList<Error>? Errors)
    {
        public static GetAmountOfDiagnosisWithICDCodePayload Create(int amount)
            => new(true, amount, default);

        public static GetAmountOfDiagnosisWithICDCodePayload Invalid(
            IReadOnlyList<Error> errors)
            => new(false, 0, errors);
    }
}