using Register.DTO;
using Register.Repositories;

namespace Register.IntegrationTests;

public sealed class InMemoryDiagnosisRepository : IDiagnosisRepository
{
    private readonly List<Diagnosis> _diagnoses = new();

    private static readonly Dictionary<(string System, string Code), string> KnownCodes = new()
    {
        [("http://hl7.org/fhir/sid/icd-10", "A00")] = "Cholera",
        [("http://hl7.org/fhir/sid/icd-10", "A00.1")] = "Typhoid and paratyphoid fevers",
        [("http://hl7.org/fhir/sid/icd-10", "A02.0")] = "Salmonella enteritis",
        [("http://hl7.org/fhir/sid/icd-10", "A03")] = "Shigellosis",
        [("http://hl7.org/fhir/sid/icd-10", "A04")] = "Other bacterial intestinal infections",
    };

    public IEnumerable<Diagnosis> FindByStatus(Guid patientId, ClinicalStatus status)
        => _diagnoses
            .Where(x => x.Patient.Id == patientId && x.ClinicalStatus == status)
            .ToList();

    public Diagnosis? FindBySourceIdentity(string sourceSystem, string sourceRecordId)
        => _diagnoses.FirstOrDefault(x =>
            x.SourceSystem == sourceSystem &&
            x.SourceRecordId == sourceRecordId);

    public void Add(Diagnosis diagnosis) => _diagnoses.Add(diagnosis);

    public void Seed(params Diagnosis[] diagnoses) => _diagnoses.AddRange(diagnoses);

    public string? GetDescriptionForCode(string system, string code)
        => KnownCodes.TryGetValue((system, code), out var display) ? display : null;
}
