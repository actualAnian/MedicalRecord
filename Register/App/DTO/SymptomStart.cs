using System.Text.Json.Serialization;

namespace Register.App.DTO
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(FromDate), "fromDate")]
    [JsonDerivedType(typeof(PatientAge), "patientAge")]
    public record SymptomStart
    {
        public sealed record FromDate(DateOnly Date) : SymptomStart;
        public sealed record PatientAge(int Age) : SymptomStart;
    }
}
