using Register.DTO;

namespace Register.API
{
    public class DiagnosisMutation
    {
        public RegisterDiagnosisPayload RegisterDiagnosis(
            RegisterDiagnosisRequest input,
            [Service] IDiagnosisService service)
        {
            var result = service.Register(input);
            if (result.IsFailure) return RegisterDiagnosisPayload.Invalid(result.Errors);
            else return RegisterDiagnosisPayload.Created(result.Value!);
        }
    }

    public record RegisterDiagnosisPayload(
        bool Success,
        Diagnosis? Diagnosis,
        IReadOnlyList<Error>? Errors)
        {
            public static RegisterDiagnosisPayload Created(
                Diagnosis diagnosis)
                => new(true, diagnosis, default);

            public static RegisterDiagnosisPayload Invalid(
                IReadOnlyList<Error> error)
                => new(false, null, error);
        }
}
