using Register.App.DTO;

namespace Register.DataBase.Repositories
{
    public interface IDiagnosisRepository
    {
        Task AddAsync(Diagnosis diagnosis);
        Task<IEnumerable<Diagnosis>> FindByStatusAsync(Guid patientId, ClinicalStatus status);
        Task<Diagnosis?> FindBySourceIdentityAsync(string sourceSystem, string sourceRecordId);
        Task<Diagnosis?> FindByIdAsync(Guid id);
        Task UpdateAsync(Diagnosis diagnosis);
    }
}