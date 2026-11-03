using Register.App;
using Register.App.DTO;
using Register.App.ExternalAPI;
using Register.App.Services;
using Register.DataBase.Repositories;

namespace Register.UnitTests;

public class DiagnosisServiceTests
{
    private const string CodingSystem = "http://hl7.org/fhir/sid/icd-10";
    private const string ValidIcdCode = "A00";
    private const string ValidPesel = "44051401359";
    private const string ValidMedicalRecordNumber = "MR-123";
    private static readonly Guid PatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task FindDiagnosesByStatus_ValidStatus_ReturnsRepositoryResults()
    {
        var expected = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ByStatus = [expected] };
        var service = CreateService(repository);

        var result = await service.FindDiagnosesByStatus(PatientId, "Active");

        Assert.True(result.IsSuccess);
        Assert.Same(expected, Assert.Single(result.Value));
    }

    [Fact]
    public async Task FindDiagnosesByStatus_StatusIsCaseInsensitive_ReturnsSuccess()
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = CreateService(repository);

        var result = await service.FindDiagnosesByStatus(PatientId, "active");

        Assert.True(result.IsSuccess);
        Assert.Equal(ClinicalStatus.Active, repository.LastStatus);
    }

    [Fact]
    public async Task FindDiagnosesByStatus_PassesPatientIdAndParsedStatusToRepository()
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = CreateService(repository);

        await service.FindDiagnosesByStatus(PatientId, "Healed");

        Assert.Equal(PatientId, repository.LastPatientId);
        Assert.Equal(ClinicalStatus.Healed, repository.LastStatus);
    }

    [Fact]
    public async Task FindDiagnosesByStatus_UnknownStatus_ReturnsValidationError()
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = CreateService(repository);

        var result = await service.FindDiagnosesByStatus(PatientId, "Unknown");

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Contains(nameof(ClinicalStatus.Active), error.Message);
        Assert.Contains(nameof(ClinicalStatus.Healed), error.Message);
        Assert.Contains(nameof(ClinicalStatus.Return), error.Message);
        Assert.Null(repository.LastPatientId);
    }

    [Fact]
    public async Task Register_NewDiagnosis_ReturnsSuccessAndPersists()
    {
        var patient = new Patient(PatientId, ValidPesel);
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(patient));
        var service = CreateService(repository, patients);

        var result = await service.Register(Request());

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(repository.Added);
        Assert.Equal(PatientId, saved.PatientId);
        Assert.Equal(ValidPesel, patients.LastPesel);
        Assert.Null(patients.LastSourceRecordId);
    }

    [Fact]
    public async Task Register_DuplicateSourceIdentity_ReturnsExistingWithoutAdding()
    {
        var existing = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = existing };
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(Request());

        Assert.True(result.IsSuccess);
        Assert.Same(existing, result.Value);
        Assert.Empty(repository.Added);
        Assert.Null(patients.LastPesel);
    }

    [Fact]
    public async Task Register_UnknownIcdCode_ReturnsNotFound()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(Request(icdCode: "ZZZ-UNKNOWN"));

        Assert.True(result.IsFailure);
        Assert.IsType<Error.NotFound>(Assert.Single(result.Errors));
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Register_MissingSymptomStart_ReturnsValidationError()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request() with { SymptomStartDate = null, SymptomStartAge = null });

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Contains("symptom start", error.Message);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Register_NoPatientIdentifier_ReturnsValidationError()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request(patientPesel: null, medicalRecordNumber: null));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Contains("PatientPesel", error.Message);
        Assert.Null(patients.LastPesel);
        Assert.Null(patients.LastSourceRecordId);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Register_UsesMedicalRecordNumberWhenPeselIsMissing()
    {
        var patient = new Patient(PatientId, ValidPesel);
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(bySourceIdentity: Result<Patient>.Success(patient));
        var service = CreateService(repository, patients);
        var request = Request(patientPesel: null, medicalRecordNumber: ValidMedicalRecordNumber);

        var result = await service.Register(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(request.SourceSystem, patients.LastSourceSystem);
        Assert.Equal(ValidMedicalRecordNumber, patients.LastSourceRecordId);
        Assert.Null(patients.LastPesel);
        Assert.Single(repository.Added);
    }

    [Fact]
    public async Task Register_PatientNotFound_ReturnsLookupError()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService();
        var service = CreateService(repository, patients);

        var result = await service.Register(Request());

        Assert.True(result.IsFailure);
        Assert.IsType<Error.NotFoundOnLookup>(Assert.Single(result.Errors));
        Assert.Empty(repository.Added);
    }

    private static DiagnosisService CreateService(
        FakeDiagnosisRepository repository,
        FakePatientService? patientService = null,
        FakeIcd10Catalogue? catalogue = null,
        FakeExternalRegistryClient? externalRegistry = null)
        => new(
            repository,
            patientService ?? new FakePatientService(),
            catalogue ?? new FakeIcd10Catalogue(),
            externalRegistry ?? new FakeExternalRegistryClient());

    private static RegisterDiagnosisRequest Request(
        string? patientPesel = ValidPesel,
        string? medicalRecordNumber = null,
        string icdCode = ValidIcdCode)
        => new(
            SourceSystem: "HIS",
            SourceRecordId: Guid.NewGuid().ToString("N"),
            PatientPesel: patientPesel,
            MedicalRecordNumber: medicalRecordNumber,
            IcdCode: icdCode,
            CodingSystem: CodingSystem,
            DiagnosisDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ClinicalStatus: "Active",
            ConfirmationStatus: "Confirmed",
            SymptomStartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            SymptomStartAge: null,
            VisitId: "VISIT-1");

    private static Diagnosis DiagnosisFor(Guid patientId, ClinicalStatus status) => new(
        Guid.NewGuid(),
        "HIS",
        Guid.NewGuid().ToString("N"),
        patientId,
        new ICD10(CodingSystem, ValidIcdCode, "Cholera"),
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
        public List<Diagnosis> Added { get; } = [];

        public Task<IEnumerable<Diagnosis>> FindByStatusAsync(Guid patientId, ClinicalStatus status)
        {
            LastPatientId = patientId;
            LastStatus = status;
            return Task.FromResult(ByStatus);
        }

        public Task<Diagnosis?> FindBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
            => Task.FromResult(ExistingDiagnosis);

        public Task AddAsync(Diagnosis diagnosis)
        {
            Added.Add(diagnosis);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePatientService(
        Result<Patient>? byPesel = null,
        Result<Patient>? bySourceIdentity = null) : IPatientService
    {
        private readonly Result<Patient> _byPesel = byPesel
            ?? Result<Patient>.Fail([new Error.NotFoundOnLookup("Patient", "not found")]);
        private readonly Result<Patient> _bySourceIdentity = bySourceIdentity
            ?? Result<Patient>.Fail([new Error.NotFoundOnLookup("Patient", "not found")]);

        public string? LastPesel { get; private set; }
        public string? LastSourceSystem { get; private set; }
        public string? LastSourceRecordId { get; private set; }

        public Task<Result<Patient>> FindPatientByPeselAsync(string pesel)
        {
            LastPesel = pesel;
            return Task.FromResult(_byPesel);
        }

        public Task<Result<Patient>> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        {
            LastSourceSystem = sourceSystem;
            LastSourceRecordId = sourceRecordId;
            return Task.FromResult(_bySourceIdentity);
        }
    }

    private sealed class FakeIcd10Catalogue : IICD10Catalogue
    {
        private static readonly Dictionary<(string System, string Code), string> KnownCodes = new()
        {
            [(CodingSystem, ValidIcdCode)] = "Cholera"
        };

        public ICD10? Find(string system, string code)
            => KnownCodes.TryGetValue((system, code), out var display)
                ? new ICD10(system, code, display)
                : null;

        public string? GetDescriptionForCode(string system, string code)
            => KnownCodes.TryGetValue((system, code), out var display) ? display : null;
    }

    private sealed class FakeExternalRegistryClient : IExternalRegistryClient
    {
        public Task<Result<Diagnosis>> RegisterAsync(
            Diagnosis diagnosis,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result<Diagnosis>.Success(diagnosis));
    }
}
