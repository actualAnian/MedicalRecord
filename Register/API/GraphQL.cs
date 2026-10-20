using Register.DTO;

namespace Register.API
{
    public class DiagnosisMutation
    {
        public async Task<RegisterDiagnosisPayload> RegisterDiagnosis(
            RegisterDiagnosisRequest input,
            [Service] IDiagnosisService service)
        {
            var result = await service.Register(input);
            if (result.IsFailure) return RegisterDiagnosisPayload.Invalid(result.Errors);
            else return RegisterDiagnosisPayload.Created();
        }
    }

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
