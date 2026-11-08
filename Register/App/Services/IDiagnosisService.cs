using Register.App.DTO;

namespace Register.App.Services
{
    public interface IDiagnosisService
    {
        Task<Result<IEnumerable<Diagnosis>>> FindDiagnosesByStatus(Guid patientId, string clinicalStatuses);
        Task<Result<Diagnosis>> Register(RegisterDiagnosisRequest request, CancellationToken cancellationToken = default);
        Task<Result<Diagnosis>> ChangeStatus(ChangeDiagnosisStatusRequest request, CancellationToken cancellationToken = default);
        Task<Result<int>> GetAmountOfDiagnosisWithICDCode(string icdSystem, string icdCode);
    }
}
