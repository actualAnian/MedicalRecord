using Register.App.DTO;
using Register.App.Services;

namespace Register.App.Endpoints
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

        public async Task<ChangeDiagnosisStatusPayload> ChangeDiagnosisStatus(
            ChangeDiagnosisStatusRequest input,
            [Service] IDiagnosisService service)
        {
            var result = await service.ChangeStatus(input);
            if (result.IsFailure) return ChangeDiagnosisStatusPayload.Invalid(result.Errors);
            else return ChangeDiagnosisStatusPayload.Updated(result.GetValueOrThrow());
        }
    }
}
