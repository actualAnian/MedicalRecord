using Register.App.DTO;
using Register.DataBase.Repositories;

namespace Register.IntegrationTests;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly Dictionary<string, Patient> _byPesel = new();
    private readonly Dictionary<(string SourceSystem, string SourceRecordId), Patient> _bySourceIdentity = new();

    public void Seed(Patient patient, string pesel, string sourceSystem, string sourceRecordId)
    {
        _byPesel[pesel] = patient;
        _bySourceIdentity[(sourceSystem, sourceRecordId)] = patient;
    }

    public Task<Patient?> FindPatientByPeselAsync(string pesel)
        => Task.FromResult(_byPesel.TryGetValue(pesel, out var patient) ? patient : null);

    public Task<Patient?> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        => Task.FromResult(_bySourceIdentity.TryGetValue((sourceSystem, sourceRecordId), out var patient) ? patient : null);
}
