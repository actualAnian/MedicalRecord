using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Register.App.DTO;

namespace Register.IntegrationTests;

// Covers MapDiagnosis(): POST /diagnoses (OpenAPI / REST)
public class RestDiagnosisTests
{
    [Fact]
    public async Task MapDiagnosis_ValidRequest_PersistsDiagnosis()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId);

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(DiagnosisServer.ValidPatientId, saved.PatientId);
        Assert.Equal(DiagnosisServer.ValidIcdCode, saved.IcdCode.Code);
        Assert.Equal(ClinicalStatus.Active, saved.ClinicalStatus);
        Assert.Equal(ConfirmationStatus.Confirmed, saved.ConfirmationStatus);
    }

    [Fact]
    public async Task MapDiagnosis_DuplicateSourceIdentity_ReturnsExistingDiagnosis()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var first = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId);
        var second = first with { PatientPesel = "44051401359" };

        var firstResponse = await client.PostAsJsonAsync("/diagnoses", first);
        var secondResponse = await client.PostAsJsonAsync("/diagnoses", second);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(
            await ReadDiagnosisIdAsync(firstResponse),
            await ReadDiagnosisIdAsync(secondResponse));
    }

    [Fact]
    public async Task MapDiagnosis_UnknownIcdCode_ReturnsNotFound()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(icdCode: "ZZZ-UNKNOWN");

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("NotFound", error.Code);
    }

    [Fact]
    public async Task MapDiagnosis_MissingSymptomStart_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with
        {
            SymptomStartDate = null,
            SymptomStartAge = null
        };

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("missing fields, symptom start date or symptom start age", error.Message);
    }

    [Fact]
    public async Task MapDiagnosis_BothSymptomStartValues_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with { SymptomStartAge = 42 };

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("multiple fields for symptom start", error.Message);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("99")]
    public async Task MapDiagnosis_InvalidClinicalStatus_ReturnsBadRequest(string clinicalStatus)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with { ClinicalStatus = clinicalStatus };

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", error.Message);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("99")]
    public async Task MapDiagnosis_InvalidConfirmationStatus_ReturnsBadRequest(string confirmationStatus)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with
        {
            ConfirmationStatus = confirmationStatus
        };

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal(
            "confirmation status not found, all statuses: Probable, Confirmed",
            error.Message);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public async Task MapDiagnosis_NoPatientIdentifier_ReturnsBadRequest(
        string? patientPesel,
        string? medicalRecordNumber)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(
            patientPesel: patientPesel,
            medicalRecordNumber: medicalRecordNumber);

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("PatientPesel or MedicalRecordNumber must be provided", error.Message);
    }

    [Fact]
    public async Task MapDiagnosis_SymptomStartAgeOnly_PersistsPatientAge()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId) with
        {
            SymptomStartDate = null,
            SymptomStartAge = 42
        };

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        var symptomStart = Assert.IsType<SymptomStart.PatientAge>(saved.SymptomStart);
        Assert.Equal(42, symptomStart.Age);
    }

    [Fact]
    public async Task MapDiagnosis_UnknownPatientPesel_ReturnsForbidden()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(patientPesel: "00000000000");

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("NotFoundOnLookup", error.Code);
    }

    [Fact]
    public async Task MapDiagnosis_UnknownMedicalRecordNumber_ReturnsForbidden()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(
            patientPesel: null,
            medicalRecordNumber: "MR-UNKNOWN");

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("NotFoundOnLookup", error.Code);
    }

    [Fact]
    public async Task MapDiagnosis_UsesMedicalRecordNumberWhenPeselIsMissing()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var request = DiagnosisServer.ValidRequest(
            sourceRecordId: sourceRecordId,
            patientPesel: null);

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(DiagnosisServer.ValidPatientId, saved.PatientId);
    }

    [Fact]
    public async Task MapDiagnosis_PeselTakesPrecedenceOverMedicalRecordNumber()
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

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await DiagnosisServer.FindDiagnosisAsync(
            app, request.SourceSystem, sourceRecordId);
        Assert.NotNull(saved);
        Assert.Equal(DiagnosisServer.ValidPatientId, saved.PatientId);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_ValidSourceIdentity_UpdatesDiagnosis()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-STATUS");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await client.PostAsJsonAsync("/diagnoses/status",
            new
            {
                sourceSystem = diagnosis.SourceSystem,
                sourceDiagnosisRecordId = diagnosis.SourceRecordId,
                clinicalStatus = "Healed"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await DiagnosisServer.FindDiagnosisAsync(app, diagnosis.Id);
        Assert.NotNull(updated);
        Assert.Equal(ClinicalStatus.Healed, updated.ClinicalStatus);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_AlreadyHealed_ReturnsBadRequestWithMessage()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Healed, "D-HEALED");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await client.PostAsJsonAsync("/diagnoses/status",
            new
            {
                sourceSystem = diagnosis.SourceSystem,
                sourceDiagnosisRecordId = diagnosis.SourceRecordId,
                clinicalStatus = "Healed"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("Diagnosis is already healed.", error.Message);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_DiagnosisNotFound_ReturnsForbiddenWithMessage()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.PostAsJsonAsync("/diagnoses/status",
            new
            {
                sourceSystem = DiagnosisServer.ValidSourceSystem,
                sourceDiagnosisRecordId = "D-MISSING",
                clinicalStatus = "Healed"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("NotFoundOnLookup", error.Code);
        Assert.Equal(
            "Diagnosis not found. Message: Diagnosis with ID D-MISSING not found",
            error.Message);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_UnsupportedCurrentState_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Return, "D-RETURN");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await client.PostAsJsonAsync("/diagnoses/status",
            new
            {
                sourceSystem = diagnosis.SourceSystem,
                sourceDiagnosisRecordId = diagnosis.SourceRecordId,
                clinicalStatus = "Healed"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("Invalid command for current diagnosis state.", error.Message);
    }

    [Theory]
    [InlineData("Active", "Unsupported status transition to Active.")]
    [InlineData("Return", "Unsupported status transition to Return.")]
    public async Task ChangeDiagnosisStatus_UnsupportedTargetStatus_ReturnsBadRequest(
        string clinicalStatus,
        string expectedMessage)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-UNSUPPORTED");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await client.PostAsJsonAsync("/diagnoses/status",
            new
            {
                sourceSystem = diagnosis.SourceSystem,
                sourceDiagnosisRecordId = diagnosis.SourceRecordId,
                clinicalStatus
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal(expectedMessage, error.Message);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ChangeDiagnosisStatus_UnknownStatus_ReturnsBadRequest(string clinicalStatus)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-UNKNOWN-STATUS");
        await DiagnosisServer.AddDiagnosesAsync(app, diagnosis);

        var response = await client.PostAsJsonAsync("/diagnoses/status",
            new
            {
                sourceSystem = diagnosis.SourceSystem,
                sourceDiagnosisRecordId = diagnosis.SourceRecordId,
                clinicalStatus
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("status not found, all statuses: Active, Healed, Return", error.Message);
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

        var response = await client.GetAsync(
            $"/diagnoses/count?icdSystem={Uri.EscapeDataString(DiagnosisServer.ValidCodingSystem)}" +
            $"&icdCode={DiagnosisServer.ValidIcdCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var amount = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(2, amount);
    }

    [Fact]
    public async Task GetDiagnosisCount_UnknownCode_ReturnsZero()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            $"/diagnoses/count?icdSystem={Uri.EscapeDataString(DiagnosisServer.ValidCodingSystem)}" +
            "&icdCode=ZZZ-UNKNOWN");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var amount = await response.Content.ReadFromJsonAsync<int>();
        Assert.Equal(0, amount);
    }

    [Fact]
    public async Task GetDiagnosisCount_MissingInput_ReturnsBadRequestWithMessage()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync("/diagnoses/count?icdSystem=&icdCode=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
        Assert.Equal("IcdSystem and IcdCode must be provided", error.Message);
    }

    [Fact]
    public async Task GetPatientDiagnoses_ValidStatus_ReturnsOnlyMatchingDiagnoses()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var otherPatientId = Guid.NewGuid();
        await DiagnosisServer.AddPatientAsync(otherPatientId, "02070803628");
        await DiagnosisServer.AddDiagnosesAsync(
            app,
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-ACTIVE"),
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Healed, "D-HEALED"),
            DiagnosisServer.DiagnosisFor(
                otherPatientId, ClinicalStatus.Active, "D-OTHER-PATIENT"));

        var response = await client.GetAsync(
            $"/patients/{DiagnosisServer.ValidPatientId}/diagnoses?clinicalStatuses=Active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var diagnosis = Assert.Single(body.RootElement.EnumerateArray().ToList());
        Assert.Equal("D-ACTIVE", diagnosis.GetProperty("sourceRecordId").GetString());
    }

    [Fact]
    public async Task GetPatientDiagnoses_StatusIsCaseInsensitive_ReturnsMatchingDiagnoses()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        await DiagnosisServer.AddDiagnosesAsync(
            app,
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-CASE"));

        var response = await client.GetAsync(
            $"/patients/{DiagnosisServer.ValidPatientId}/diagnoses?clinicalStatuses=active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var diagnosis = Assert.Single(body.RootElement.EnumerateArray().ToList());
        Assert.Equal("D-CASE", diagnosis.GetProperty("sourceRecordId").GetString());
    }

    [Fact]
    public async Task GetPatientDiagnoses_UnknownPatient_ReturnsEmptyList()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            $"/patients/{Guid.NewGuid()}/diagnoses?clinicalStatuses=Active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Empty(body.RootElement.EnumerateArray());
    }

    [Fact]
    public async Task GetPatientDiagnoses_UnknownStatus_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            $"/patients/{DiagnosisServer.ValidPatientId}/diagnoses?clinicalStatuses=Unknown");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
    }

    [Fact]
    public async Task GetPatientDiagnoses_BlankStatus_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            $"/patients/{DiagnosisServer.ValidPatientId}/diagnoses?clinicalStatuses=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadSingleErrorAsync(response);
        Assert.Equal("ValidationError", error.Code);
    }

    [Fact]
    public async Task GetPatientDiagnoses_MissingStatus_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            $"/patients/{DiagnosisServer.ValidPatientId}/diagnoses");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<(string Code, string Message)> ReadSingleErrorAsync(
        HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = Assert.Single(
            body.RootElement.GetProperty("errors").EnumerateArray().ToList());
        return (
            error.GetProperty("code").GetString()!,
            error.GetProperty("message").GetString()!);
    }

    private static async Task<Guid> ReadDiagnosisIdAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }
}
