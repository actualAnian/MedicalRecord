namespace Register.DTO
{
    public record SymptomStart
    {
        public sealed record FromDate(DateOnly Date) : SymptomStart;
        public sealed record PatientAge(int Age) : SymptomStart;
    }
}
