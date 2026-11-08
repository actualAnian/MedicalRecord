using Register.App.DTO;
using Register.App.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace Register.IntegrationTests;

// Covers RegisterDiagnosis(): POST /graphql mutation
public class GraphQlDiagnosisTests
{
    private const string Mutation = """
        mutation Register($input: RegisterDiagnosisRequestInput!) {
          registerDiagnosis(input: $input) {
            success
            errors { code message }
          }
        }
        """;

    private const string ChangeStatusMutation = """
        mutation ChangeStatus($input: ChangeDiagnosisStatusRequestInput!) {
          changeDiagnosisStatus(input: $input) {
            success
            errors { code message }
          }
        }
        """;

    private const string CountQuery = """
        query Count($input: GetDiagnosisCountRequestInput!) {
          amountOfDiagnosisWithICDCode(input: $input) {
            success
            amount
            errors { code message }
          }
        }
        """;

    private static object ToVariables(RegisterDiagnosisRequest request) => new
    {
        input = new
        {
            sourceSystem = request.SourceSystem,
            sourceRecordId = request.SourceRecordId,
            patientPesel = request.PatientPesel,
            medicalRecordNumber = request.MedicalRecordNumber,
            icdCode = request.IcdCode,
            codingSystem = request.CodingSystem,
            diagnosisDate = request.DiagnosisDate.ToString("yyyy-MM-dd"),
            clinicalStatus = request.ClinicalStatus,
            confirmationStatus = request.ConfirmationStatus,
            symptomStartDate = request.SymptomStartDate?.ToString("yyyy-MM-dd"),
            symptomStartAge = request.SymptomStartAge,
            visitId = request.VisitId
        }
    };

    private static async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> RegisterAsync(
        HttpClient client,
        RegisterDiagnosisRequest request)
    {
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = Mutation,
            variables = ToVariables(request)
        });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GraphQL HTTP {(int)response.StatusCode} body: {body}");

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("errors", out var gqlErrors))
            throw new InvalidOperationException($"GraphQL errors: {gqlErrors}");
        var root = doc.RootElement.GetProperty("data").GetProperty("registerDiagnosis");
        var success = root.GetProperty("success").GetBoolean();
        var (errorCode, errorMessage) = ReadFirstError(root);
        return (success, errorCode, errorMessage);
    }

    private static async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> ChangeStatusAsync(
        HttpClient client,
        string sourceSystem,
        string sourceDiagnosisRecordId,
        string clinicalStatus = "Healed")
    {
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = ChangeStatusMutation,
            variables = new
            {
                input = new
                {
                    sourceSystem,
                    sourceDiagnosisRecordId,
                    clinicalStatus
                }
            }
        });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GraphQL HTTP {(int)response.StatusCode} body: {body}");

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("errors", out var gqlErrors))
            throw new InvalidOperationException($"GraphQL errors: {gqlErrors}");
        var root = doc.RootElement.GetProperty("data").GetProperty("changeDiagnosisStatus");
        var success = root.GetProperty("success").GetBoolean();
        var (errorCode, errorMessage) = ReadFirstError(root);
        return (success, errorCode, errorMessage);
    }

    private static async Task<(bool Success, int Amount, string? ErrorCode, string? ErrorMessage)> GetCountAsync(
        HttpClient client,
        string icdSystem,
        string icdCode)
    {
        var response = await client.PostAsJsonAsync("/graphql", new
        {
            query = CountQuery,
            variables = new
            {
                input = new
                {
                    icdSystem,
                    icdCode
                }
            }
        });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GraphQL HTTP {(int)response.StatusCode} body: {body}");

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("errors", out var gqlErrors))
            throw new InvalidOperationException($"GraphQL errors: {gqlErrors}");
        var root = doc.RootElement.GetProperty("data").GetProperty("amountOfDiagnosisWithICDCode");
        var success = root.GetProperty("success").GetBoolean();
        var amount = root.GetProperty("amount").GetInt32();
        var (errorCode, errorMessage) = ReadFirstError(root);
        return (success, amount, errorCode, errorMessage);
    }

    private static (string? Code, string? Message) ReadFirstError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) ||
            errors.ValueKind != JsonValueKind.Array)
            return (null, null);

        var firstError = errors.EnumerateArray().FirstOrDefault();
        return firstError.ValueKind == JsonValueKind.Undefined
            ? (null, null)
            : (
                firstError.GetProperty("code").GetString(),
                firstError.GetProperty("message").GetString());
    }

    [Fact]
    public async Task RegisterDiagnosis_ValidRequest_PersistsDiagnosis()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId);

        var response = await RegisterAsync(client, request);

        Assert.True(response.Success);
        Assert.Null(response.ErrorCode);
        Assert.Null(response.ErrorMessage);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(DiagnosisServer.ValidPatientId, saved.PatientId);
        Assert.Equal(DiagnosisServer.ValidIcdCode, saved.IcdCode.Code);
    }

    [Fact]
    public async Task RegisterDiagnosis_DuplicateSourceIdentity_ReturnsSuccess()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var first = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId);
        var second = first with { PatientPesel = "44051401359" };

        var firstResponse = await RegisterAsync(client, first);
        var secondResponse = await RegisterAsync(client, second);

        Assert.True(firstResponse.Success);
        Assert.True(secondResponse.Success);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, first.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task RegisterDiagnosis_UnknownIcdCode_ReturnsNotFoundError()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await RegisterAsync(
            client, DiagnosisServer.ValidRequest(icdCode: "ZZZ-UNKNOWN"));

        Assert.False(response.Success);
        Assert.Equal("NotFound", response.ErrorCode);
    }

    [Fact]
    public async Task RegisterDiagnosis_MissingSymptomStart_ReturnsValidationError()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with
        {
            SymptomStartDate = null,
            SymptomStartAge = null
        };

        var response = await RegisterAsync(client, request);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("missing fields, symptom start date or symptom start age", response.ErrorMessage);
    }

    [Fact]
    public async Task RegisterDiagnosis_BothSymptomStartValues_ReturnsValidationError()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with { SymptomStartAge = 42 };

        var response = await RegisterAsync(client, request);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("multiple fields for symptom start", response.ErrorMessage);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("99")]
    public async Task RegisterDiagnosis_InvalidClinicalStatus_ReturnsValidationError(
        string clinicalStatus)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with { ClinicalStatus = clinicalStatus };

        var response = await RegisterAsync(client, request);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", response.ErrorMessage);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("99")]
    public async Task RegisterDiagnosis_InvalidConfirmationStatus_ReturnsValidationError(
        string confirmationStatus)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with
        {
            ConfirmationStatus = confirmationStatus
        };

        var response = await RegisterAsync(client, request);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal(
            "confirmation status not found, all statuses: Probable, Confirmed",
            response.ErrorMessage);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public async Task RegisterDiagnosis_NoPatientIdentifier_ReturnsValidationError(
        string? patientPesel,
        string? medicalRecordNumber)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(
            patientPesel: patientPesel,
            medicalRecordNumber: medicalRecordNumber);

        var response = await RegisterAsync(client, request);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("PatientPesel or MedicalRecordNumber must be provided", response.ErrorMessage);
    }

    [Fact]
    public async Task RegisterDiagnosis_UnknownPatientPesel_ReturnsLookupError()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await RegisterAsync(
            client, DiagnosisServer.ValidRequest(patientPesel: "00000000000"));

        Assert.False(response.Success);
        Assert.Equal("NotFoundOnLookup", response.ErrorCode);
    }

    [Fact]
    public async Task RegisterDiagnosis_UnknownMedicalRecordNumber_ReturnsLookupError()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(
            patientPesel: null,
            medicalRecordNumber: "MR-UNKNOWN");

        var response = await RegisterAsync(client, request);

        Assert.False(response.Success);
        Assert.Equal("NotFoundOnLookup", response.ErrorCode);
    }

    [Fact]
    public async Task RegisterDiagnosis_UsesMedicalRecordNumberWhenPeselIsMissing()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(
            sourceRecordId: sourceRecordId,
            patientPesel: null);

        var response = await RegisterAsync(client, request);

        Assert.True(response.Success);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(DiagnosisServer.ValidPatientId, saved.PatientId);
    }

    [Fact]
    public async Task RegisterDiagnosis_PeselTakesPrecedenceOverMedicalRecordNumber()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var otherPatientId = Guid.NewGuid();
        await DiagnosisServer.AddPatientAsync(
            otherPatientId,
            "02070803628",
            ("HIS", "MR-OTHER"));
        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(
            sourceRecordId: sourceRecordId,
            medicalRecordNumber: "MR-OTHER");

        var response = await RegisterAsync(client, request);

        Assert.True(response.Success);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(DiagnosisServer.ValidPatientId, saved.PatientId);
    }

    [Fact]
    public async Task RegisterDiagnosis_ValidStatusValues_PersistsStatuses()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId) with
        {
            ClinicalStatus = "Healed",
            ConfirmationStatus = "Probable"
        };

        var response = await RegisterAsync(client, request);

        Assert.True(response.Success);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(ClinicalStatus.Healed, saved.ClinicalStatus);
        Assert.Equal(ConfirmationStatus.Probable, saved.ConfirmationStatus);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_ActiveDiagnosis_ReturnsSuccessTrue()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-STATUS");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await ChangeStatusAsync(
            client, diagnosis.SourceSystem, diagnosis.SourceRecordId);

        Assert.True(response.Success);
        Assert.Null(response.ErrorCode);
        Assert.Null(response.ErrorMessage);
        var updated = await DiagnosisServer.FindDiagnosisAsync(app, diagnosis.Id);
        Assert.NotNull(updated);
        Assert.Equal(ClinicalStatus.Healed, updated.ClinicalStatus);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_AlreadyHealed_ReturnsErrorMessage()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Healed, "D-HEALED");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await ChangeStatusAsync(
            client, diagnosis.SourceSystem, diagnosis.SourceRecordId);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("Diagnosis is already healed.", response.ErrorMessage);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_DiagnosisNotFound_ReturnsErrorMessage()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await ChangeStatusAsync(
            client, DiagnosisServer.ValidSourceSystem, "D-MISSING");

        Assert.False(response.Success);
        Assert.Equal("NotFoundOnLookup", response.ErrorCode);
        Assert.Equal(
            "Diagnosis not found. Message: Diagnosis with ID D-MISSING not found",
            response.ErrorMessage);
    }

    [Theory]
    [InlineData("Active", "Unsupported status transition to Active.")]
    [InlineData("Return", "Unsupported status transition to Return.")]
    public async Task ChangeDiagnosisStatus_UnsupportedTargetStatus_ReturnsErrorMessage(
        string clinicalStatus,
        string expectedMessage)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-UNSUPPORTED");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await ChangeStatusAsync(
            client,
            diagnosis.SourceSystem,
            diagnosis.SourceRecordId,
            clinicalStatus);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal(expectedMessage, response.ErrorMessage);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ChangeDiagnosisStatus_UnknownStatus_ReturnsErrorMessage(
        string clinicalStatus)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-UNKNOWN-STATUS");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await ChangeStatusAsync(
            client,
            diagnosis.SourceSystem,
            diagnosis.SourceRecordId,
            clinicalStatus);

        Assert.False(response.Success);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", response.ErrorMessage);
    }

    [Fact]
    public async Task GetDiagnosisCount_MatchingSystemAndCode_ReturnsCount()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        await DiagnosisServer.AddDiagnosesAsync(
            app,
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-COUNT-1"),
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Healed, "D-COUNT-2"),
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-OTHER-CODE",
                icdCode: "A00.1"),
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-OTHER-SYSTEM",
                icdSystem: "OTHER"));

        var response = await GetCountAsync(
            client, DiagnosisServer.ValidCodingSystem, DiagnosisServer.ValidIcdCode);

        Assert.True(response.Success);
        Assert.Equal(2, response.Amount);
        Assert.Null(response.ErrorCode);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task GetDiagnosisCount_UnknownCode_ReturnsZero()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await GetCountAsync(
            client, DiagnosisServer.ValidCodingSystem, "ZZZ-UNKNOWN");

        Assert.True(response.Success);
        Assert.Equal(0, response.Amount);
        Assert.Null(response.ErrorCode);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task GetDiagnosisCount_MissingInput_ReturnsErrorMessage()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await GetCountAsync(client, "", "");

        Assert.False(response.Success);
        Assert.Equal(0, response.Amount);
        Assert.Equal("ValidationError", response.ErrorCode);
        Assert.Equal("IcdSystem and IcdCode must be provided", response.ErrorMessage);
    }
}
