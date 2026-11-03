using Register.App.DTO;
using Register.DataBase.Repositories;

namespace Register.IntegrationTests;

public sealed class InMemoryDiagnosisRepository : IDiagnosisRepository
{
    private readonly List<Diagnosis> _diagnoses = new();

    public Task<IEnumerable<Diagnosis>> FindByStatusAsync(Guid patientId, ClinicalStatus status)
        => Task.FromResult<IEnumerable<Diagnosis>>(
            _diagnoses
                .Where(x => x.PatientId == patientId && x.ClinicalStatus == status)
                .ToList());

    public Task<Diagnosis?> FindBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        => Task.FromResult(_diagnoses.FirstOrDefault(x =>
            x.SourceSystem == sourceSystem &&
            x.SourceRecordId == sourceRecordId));

    public Task AddAsync(Diagnosis diagnosis)
    {
        _diagnoses.Add(diagnosis);
        return Task.CompletedTask;
    }

    public void Seed(params Diagnosis[] diagnoses) => _diagnoses.AddRange(diagnoses);
}
