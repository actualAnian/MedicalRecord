namespace Register.DTO
{
    public abstract record SymptomStart
    {
        public sealed record FromDate(DateOnly Date) : SymptomStart;
        public sealed record PatientAge(int Age) : SymptomStart;
    }
}
