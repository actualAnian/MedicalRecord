using Register.DTO;
using Register.ExternalAPI;
using Register.Repositories;
using Register.Services;

namespace Register.UnitTests;

public class DiagnosisServiceTests
{
    private static readonly Guid PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void FindDiagnosesByStatus_ValidStatus_ReturnsRepositoryResults()
    {
        var expected = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ByStatus = [expected] };
        var service = new DiagnosisService(repository, new FakeExternalRegistryClient());

        var result = service.FindDiagnosesByStatus(PatientId, "Active");

        Assert.True(result.IsSuccess);
        Assert.Same(expected, Assert.Single(result.Value));
    }

    [Fact]
    public void FindDiagnosesByStatus_StatusIsCaseInsensitive_ReturnsSuccess()
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = new DiagnosisService(repository, new FakeExternalRegistryClient());

        var result = service.FindDiagnosesByStatus(PatientId, "active");

        Assert.True(result.IsSuccess);
        Assert.Equal(ClinicalStatus.Active, repository.LastStatus);
    }

    [Fact]
    public void FindDiagnosesByStatus_PassesPatientIdAndParsedStatusToRepository()
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = new DiagnosisService(repository, new FakeExternalRegistryClient());

        service.FindDiagnosesByStatus(PatientId, "Healed");

        Assert.Equal(PatientId, repository.LastPatientId);
        Assert.Equal(ClinicalStatus.Healed, repository.LastStatus);
    }

    [Fact]
    public void FindDiagnosesByStatus_UnknownStatus_ReturnsValidationError()
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = new DiagnosisService(repository, new FakeExternalRegistryClient());

        var result = service.FindDiagnosesByStatus(PatientId, "Unknown");

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Contains(nameof(ClinicalStatus.Active), error.Message);
        Assert.Contains(nameof(ClinicalStatus.Healed), error.Message);
        Assert.Contains(nameof(ClinicalStatus.Return), error.Message);
        Assert.Null(repository.LastPatientId);
    }

    private static Diagnosis DiagnosisFor(Guid patientId, ClinicalStatus status) => new(
        "HIS",
        Guid.NewGuid().ToString("N"),
        new Patient(patientId),
        new ICD10("http://hl7.org/fhir/sid/icd-10", "A00", "Cholera"),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
        status,
        ConfirmationStatus.Confirmed,
        new SymptomStart.FromDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))));

    private sealed class FakeDiagnosisRepository : IDiagnosisRepository
    {
        public IEnumerable<Diagnosis> ByStatus { get; init; } = [];
        public Diagnosis? ExistingDiagnosis { get; init; }
        public Guid? LastPatientId { get; private set; }
        public ClinicalStatus? LastStatus { get; private set; }

        public IEnumerable<Diagnosis> FindByStatus(Guid patientId, ClinicalStatus status)
        {
            LastPatientId = patientId;
            LastStatus = status;
            return ByStatus;
        }

        public Diagnosis? FindBySourceIdentity(string sourceSystem, string sourceRecordId)
            => ExistingDiagnosis;

        public void Add(Diagnosis diagnosis)
        {
        }

        public string? GetDescriptionForCode(string system, string code) => "Cholera";
    }

    private sealed class FakeExternalRegistryClient : IExternalRegistryClient
    {
        public Task<Result<Diagnosis>> RegisterAsync(
            Diagnosis diagnosis,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Diagnosis>.Success(diagnosis));
    }
}
