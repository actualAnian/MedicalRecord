using Register.App.DTO;
using Register.App.Services;
using Register.DataBase.Repositories;

namespace Register.App
{
    public class DiagnosisQueries
    {
        public async Task<GetAmountOfDiagnosisWithICDCodePayload> GetAmountOfDiagnosisWithICDCode(
            GetDiagnosisCountRequest input,
            [Service] IDiagnosisService service)
        {
            var result = await service.GetAmountOfDiagnosisWithICDCode(input.IcdSystem, input.IcdCode);
            if (result.IsFailure) return GetAmountOfDiagnosisWithICDCodePayload.Invalid(result.Errors);
            else return GetAmountOfDiagnosisWithICDCodePayload.Create(result.GetValueOrThrow());
        }

    }
}