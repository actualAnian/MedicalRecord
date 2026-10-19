using Register.DTO;

namespace Register.Builders
{
    public class SymptomStartBuilder
    {
        public static Result<SymptomStart> BuildSymptomStart(DateOnly? symptomStartDate, int? symptomStartAge)
        {
            SymptomStart? fromDate = default;
            SymptomStart? fromAge = default;
            if (symptomStartDate.HasValue) fromDate = new SymptomStart.FromDate(symptomStartDate.Value);
            if (symptomStartAge.HasValue) fromAge = new SymptomStart.PatientAge(symptomStartAge.Value);
            if (fromDate != null && fromAge != null) return Result<SymptomStart>.Fail([new Error.Validation("multiple fields for symptom start")]);
            else if (fromDate != null)
                return Result<SymptomStart>.Success(fromDate);
            else if (fromAge != null) return Result<SymptomStart>.Success(fromAge);
            return Result<SymptomStart>.Fail([new Error.Validation("missing fields, symptom start date or symptom start age")]);
        }
        public sealed record FromDate(DateOnly Date) : SymptomStart;
        public sealed record PatientAge(int Age) : SymptomStart;
    }
}
