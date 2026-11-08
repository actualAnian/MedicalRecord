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
    private const string ValidSourceSystem = "HIS";
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task FindDiagnosesByStatus_BlankStatus_ReturnsValidationError(string status)
    {
        var repository = new FakeDiagnosisRepository { ByStatus = [] };
        var service = CreateService(repository);

        var result = await service.FindDiagnosesByStatus(PatientId, status);

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Contains(nameof(ClinicalStatus.Active), error.Message);
        Assert.Contains(nameof(ClinicalStatus.Healed), error.Message);
        Assert.Contains(nameof(ClinicalStatus.Return), error.Message);
        Assert.Null(repository.LastPatientId);
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
        var external = new FakeExternalRegistryClient();
        var service = CreateService(repository, patients, externalRegistry: external);

        var result = await service.Register(Request());

        Assert.True(result.IsSuccess);
        Assert.Same(existing, result.Value);
        Assert.Empty(repository.Added);
        Assert.Null(patients.LastPesel);
        Assert.Empty(external.Registered);
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

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("99")]
    public async Task Register_InvalidClinicalStatus_ReturnsValidationError(string clinicalStatus)
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var external = new FakeExternalRegistryClient();
        var service = CreateService(repository, patients, externalRegistry: external);

        var result = await service.Register(Request() with { ClinicalStatus = clinicalStatus });

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", error.Message);
        Assert.Empty(repository.Added);
        Assert.Empty(external.Registered);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("99")]
    public async Task Register_InvalidConfirmationStatus_ReturnsValidationError(string confirmationStatus)
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var external = new FakeExternalRegistryClient();
        var service = CreateService(repository, patients, externalRegistry: external);

        var result = await service.Register(
            Request() with { ConfirmationStatus = confirmationStatus });

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal(
            "confirmation status not found, all statuses: Probable, Confirmed",
            error.Message);
        Assert.Empty(repository.Added);
        Assert.Empty(external.Registered);
    }

    [Theory]
    [InlineData("Active", ClinicalStatus.Active)]
    [InlineData("active", ClinicalStatus.Active)]
    [InlineData("Healed", ClinicalStatus.Healed)]
    [InlineData("Return", ClinicalStatus.Return)]
    public async Task Register_ValidClinicalStatus_MapsStatus(
        string clinicalStatus,
        ClinicalStatus expected)
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request() with { ClinicalStatus = clinicalStatus });

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(repository.Added);
        Assert.Equal(expected, saved.ClinicalStatus);
    }

    [Theory]
    [InlineData("Probable", ConfirmationStatus.Probable)]
    [InlineData("probable", ConfirmationStatus.Probable)]
    [InlineData("Confirmed", ConfirmationStatus.Confirmed)]
    public async Task Register_ValidConfirmationStatus_MapsStatus(
        string confirmationStatus,
        ConfirmationStatus expected)
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request() with { ConfirmationStatus = confirmationStatus });

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(repository.Added);
        Assert.Equal(expected, saved.ConfirmationStatus);
    }

    [Fact]
    public async Task Register_SymptomStartAgeOnly_SavesPatientAge()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request() with { SymptomStartDate = null, SymptomStartAge = 42 });

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(repository.Added);
        var symptomStart = Assert.IsType<SymptomStart.PatientAge>(saved.SymptomStart);
        Assert.Equal(42, symptomStart.Age);
    }

    [Fact]
    public async Task Register_BothSymptomStartValues_ReturnsValidationError()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var external = new FakeExternalRegistryClient();
        var service = CreateService(repository, patients, externalRegistry: external);

        var result = await service.Register(Request() with { SymptomStartAge = 42 });

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("multiple fields for symptom start", error.Message);
        Assert.Empty(repository.Added);
        Assert.Empty(external.Registered);
    }

    [Fact]
    public async Task Register_MultipleValidationErrors_ReturnsAllErrors()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var external = new FakeExternalRegistryClient();
        var service = CreateService(repository, patients, externalRegistry: external);
        var request = Request(icdCode: "ZZZ-UNKNOWN") with
        {
            ClinicalStatus = "Unknown",
            ConfirmationStatus = "Unknown",
            SymptomStartAge = 42
        };

        var result = await service.Register(request);

        Assert.True(result.IsFailure);
        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, error =>
            error is Error.Validation { Message: "multiple fields for symptom start" });
        Assert.Contains(result.Errors, error =>
            error is Error.Validation { Message: "status not found, all statuses: Active, Healed, Return" });
        Assert.Contains(result.Errors, error =>
            error is Error.Validation { Message: "confirmation status not found, all statuses: Probable, Confirmed" });
        Assert.Single(result.Errors.OfType<Error.NotFound>());
        Assert.Empty(repository.Added);
        Assert.Empty(external.Registered);
    }

    [Fact]
    public async Task Register_MedicalRecordNumberPatientNotFound_ReturnsLookupError()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService();
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request(patientPesel: null, medicalRecordNumber: ValidMedicalRecordNumber));

        Assert.True(result.IsFailure);
        Assert.IsType<Error.NotFoundOnLookup>(Assert.Single(result.Errors));
        Assert.Equal(ValidMedicalRecordNumber, patients.LastSourceRecordId);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public async Task Register_PeselTakesPrecedenceOverMedicalRecordNumber()
    {
        var otherPatient = new Patient(Guid.NewGuid(), "02070803628");
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(
            byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)),
            bySourceIdentity: Result<Patient>.Success(otherPatient));
        var service = CreateService(repository, patients);

        var result = await service.Register(
            Request(medicalRecordNumber: ValidMedicalRecordNumber));

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(repository.Added);
        Assert.Equal(PatientId, saved.PatientId);
        Assert.Equal(ValidPesel, patients.LastPesel);
        Assert.Null(patients.LastSourceRecordId);
    }

    [Fact]
    public async Task Register_ExternalRegistryFailure_ReturnsExternalError()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var external = new FakeExternalRegistryClient
        {
            Response = Result<Diagnosis>.Fail([new Error.External("registry unavailable")])
        };
        var service = CreateService(repository, patients, externalRegistry: external);

        var result = await service.Register(Request());

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.External>(Assert.Single(result.Errors));
        Assert.Equal("registry unavailable", error.Message);
        Assert.Single(repository.Added);
        Assert.Single(external.Registered);
    }

    [Fact]
    public async Task Register_ExternalRegistrySuccess_CalledWithSavedDiagnosisAndToken()
    {
        var repository = new FakeDiagnosisRepository();
        var patients = new FakePatientService(byPesel: Result<Patient>.Success(new Patient(PatientId, ValidPesel)));
        var external = new FakeExternalRegistryClient();
        var service = CreateService(repository, patients, externalRegistry: external);
        using var cancellation = new CancellationTokenSource();

        var result = await service.Register(Request(), cancellation.Token);

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(repository.Added);
        var sent = Assert.Single(external.Registered);
        Assert.Same(saved, sent);
        Assert.Equal(cancellation.Token, external.LastCancellationToken);
    }

    [Fact]
    public async Task ChangeStatus_ActiveDiagnosisToHealed_UpdatesRepository()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);
        var request = ChangeStatusRequest(sourceDiagnosisRecordId: diagnosis.SourceRecordId);

        var result = await service.ChangeStatus(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClinicalStatus.Healed, result.Value.ClinicalStatus);
        Assert.Equal(diagnosis.SourceSystem, repository.LastSourceSystem);
        Assert.Equal(diagnosis.SourceRecordId, repository.LastSourceRecordId);
        var updated = Assert.Single(repository.Updated);
        Assert.Same(diagnosis, updated);
        Assert.Equal(ClinicalStatus.Healed, updated.ClinicalStatus);
    }

    [Fact]
    public async Task ChangeStatus_StatusIsCaseInsensitive_UpdatesDiagnosis()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(
                sourceDiagnosisRecordId: diagnosis.SourceRecordId,
                clinicalStatus: "healed"));

        Assert.True(result.IsSuccess);
        Assert.Equal(ClinicalStatus.Healed, diagnosis.ClinicalStatus);
        Assert.Single(repository.Updated);
    }

    [Fact]
    public async Task ChangeStatus_DiagnosisNotFound_ReturnsLookupErrorWithMessage()
    {
        var repository = new FakeDiagnosisRepository();
        var service = CreateService(repository);
        var request = ChangeStatusRequest(sourceDiagnosisRecordId: "D-MISSING");

        var result = await service.ChangeStatus(request);

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.NotFoundOnLookup>(Assert.Single(result.Errors));
        Assert.Equal("NotFoundOnLookup", error.Code);
        Assert.Equal(
            "Diagnosis not found. Message: Diagnosis with ID D-MISSING not found",
            error.Message);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ChangeStatus_AlreadyHealed_ReturnsRefusalMessageAndDoesNotUpdate()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Healed);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(sourceDiagnosisRecordId: diagnosis.SourceRecordId));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("Diagnosis is already healed.", error.Message);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ChangeStatus_UnsupportedCurrentState_ReturnsValidationMessage()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Return);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(sourceDiagnosisRecordId: diagnosis.SourceRecordId));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("Invalid command for current diagnosis state.", error.Message);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ChangeStatus_UnsupportedTargetStatus_ReturnsValidationMessage()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(
                sourceDiagnosisRecordId: diagnosis.SourceRecordId,
                clinicalStatus: "Active"));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("Unsupported status transition to Active.", error.Message);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ChangeStatus_UnsupportedTargetReturn_ReturnsValidationMessage()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(
                sourceDiagnosisRecordId: diagnosis.SourceRecordId,
                clinicalStatus: "Return"));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("Unsupported status transition to Return.", error.Message);
        Assert.Empty(repository.Updated);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ChangeStatus_BlankStatus_ReturnsValidationMessage(string clinicalStatus)
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(
                sourceDiagnosisRecordId: diagnosis.SourceRecordId,
                clinicalStatus: clinicalStatus));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", error.Message);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task ChangeStatus_UnknownStatus_ReturnsValidationMessage()
    {
        var diagnosis = DiagnosisFor(PatientId, ClinicalStatus.Active);
        var repository = new FakeDiagnosisRepository { ExistingDiagnosis = diagnosis };
        var service = CreateService(repository);

        var result = await service.ChangeStatus(
            ChangeStatusRequest(
                sourceDiagnosisRecordId: diagnosis.SourceRecordId,
                clinicalStatus: "Unknown"));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", error.Message);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public async Task GetAmountOfDiagnosisWithICDCode_ReturnsRepositoryCount()
    {
        var repository = new FakeDiagnosisRepository { DiagnosisCount = 4 };
        var service = CreateService(repository);

        var result = await service.GetAmountOfDiagnosisWithICDCode(CodingSystem, ValidIcdCode);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value);
        Assert.Equal(CodingSystem, repository.LastIcdSystem);
        Assert.Equal(ValidIcdCode, repository.LastIcdCode);
    }

    [Fact]
    public async Task GetAmountOfDiagnosisWithICDCode_NoMatches_ReturnsZero()
    {
        var repository = new FakeDiagnosisRepository();
        var service = CreateService(repository);

        var result = await service.GetAmountOfDiagnosisWithICDCode(CodingSystem, ValidIcdCode);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Theory]
    [InlineData("", ValidIcdCode)]
    [InlineData(" ", ValidIcdCode)]
    [InlineData(CodingSystem, "")]
    [InlineData(CodingSystem, " ")]
    public async Task GetAmountOfDiagnosisWithICDCode_MissingInput_ReturnsValidationMessage(
        string icdSystem,
        string icdCode)
    {
        var repository = new FakeDiagnosisRepository { DiagnosisCount = 4 };
        var service = CreateService(repository);

        var result = await service.GetAmountOfDiagnosisWithICDCode(icdSystem, icdCode);

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.Validation>(Assert.Single(result.Errors));
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("IcdSystem and IcdCode must be provided", error.Message);
        Assert.Null(repository.LastIcdSystem);
        Assert.Null(repository.LastIcdCode);
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

    private static ChangeDiagnosisStatusRequest ChangeStatusRequest(
        string sourceSystem = ValidSourceSystem,
        string sourceDiagnosisRecordId = "D-123",
        string clinicalStatus = "Healed")
        => new(sourceSystem, sourceDiagnosisRecordId, clinicalStatus);

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
        public Diagnosis? DiagnosisById { get; init; }
        public Guid? LastPatientId { get; private set; }
        public ClinicalStatus? LastStatus { get; private set; }
        public Guid? LastDiagnosisId { get; private set; }
        public string? LastSourceSystem { get; private set; }
        public string? LastSourceRecordId { get; private set; }
        public int DiagnosisCount { get; init; }
        public string? LastIcdSystem { get; private set; }
        public string? LastIcdCode { get; private set; }
        public List<Diagnosis> Added { get; } = [];
        public List<Diagnosis> Updated { get; } = [];

        public Task<IEnumerable<Diagnosis>> FindByStatusAsync(Guid patientId, ClinicalStatus status)
        {
            LastPatientId = patientId;
            LastStatus = status;
            return Task.FromResult(ByStatus);
        }

        public Task<Diagnosis?> FindBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        {
            LastSourceSystem = sourceSystem;
            LastSourceRecordId = sourceRecordId;
            return Task.FromResult(ExistingDiagnosis);
        }

        public Task<Diagnosis?> FindByIdAsync(Guid id)
        {
            LastDiagnosisId = id;
            return Task.FromResult(DiagnosisById);
        }

        public Task AddAsync(Diagnosis diagnosis)
        {
            Added.Add(diagnosis);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Diagnosis diagnosis)
        {
            Updated.Add(diagnosis);
            return Task.CompletedTask;
        }

        public Task<int> GetAmountOfDiagnosisWithICDCode(string icdSystem, string icdCode)
        {
            LastIcdSystem = icdSystem;
            LastIcdCode = icdCode;
            return Task.FromResult(DiagnosisCount);
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

    private sealed class FakeIcd10Catalogue : IICD10CatalogueService
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
        public Result<Diagnosis>? Response { get; init; }
        public List<Diagnosis> Registered { get; } = [];
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<Result<Diagnosis>> RegisterAsync(
            Diagnosis diagnosis,
            CancellationToken cancellationToken = default)
        {
            Registered.Add(diagnosis);
            LastCancellationToken = cancellationToken;
            return Task.FromResult(
                Response ?? Result<Diagnosis>.Success(diagnosis));
        }
    }
}
