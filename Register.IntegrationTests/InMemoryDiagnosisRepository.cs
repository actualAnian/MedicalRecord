using Register.DTO;

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

    public Result<Diagnosis> FindBySourceIdentity(string sourceSystem, string sourceRecordId)
    {
        var diagnosis = _diagnoses.FirstOrDefault(x =>
            x.SourceSystem == sourceSystem &&
            x.SourceRecordId == sourceRecordId);
        return diagnosis is not null
            ? Result<Diagnosis>.Success(diagnosis)
            : Result<Diagnosis>.Fail([new Error.NotFound("Diagnosis", Guid.Empty)]);
    }

    public void Add(Diagnosis diagnosis) => _diagnoses.Add(diagnosis);

    public Patient GetPatient(string system, string patientCode) => new(new());

    public string? GetDescriptionForCode(string system, string code)
        => KnownCodes.TryGetValue((system, code), out var display) ? display : null;
}
