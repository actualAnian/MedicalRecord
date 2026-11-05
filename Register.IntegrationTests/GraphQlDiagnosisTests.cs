using Microsoft.Extensions.DependencyInjection;
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

    private static async Task<bool> MutateSuccessAsync(HttpClient client, RegisterDiagnosisRequest request)
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
        return root.GetProperty("success").GetBoolean();
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

        string? errorCode = null;
        string? errorMessage = null;
        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
        {
            var firstError = errors.EnumerateArray().FirstOrDefault();
            if (firstError.ValueKind != JsonValueKind.Undefined)
            {
                errorCode = firstError.GetProperty("code").GetString();
                errorMessage = firstError.GetProperty("message").GetString();
            }
        }

        return (success, errorCode, errorMessage);
    }

    [Fact]
    public async Task RegisterDiagnosis_ValidRequest_ReturnsSuccessTrue()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var success = await MutateSuccessAsync(client, DiagnosisServer.ValidRequest());

        Assert.True(success);
    }

    [Fact]
    public async Task RegisterDiagnosis_DuplicateSourceIdentity_ReturnsSuccessTrue()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourceRecordId = Guid.NewGuid().ToString("N");
        var first = DiagnosisServer.ValidRequest(sourceRecordId: sourceRecordId);
        var second = first with { PatientPesel = "44051401359" };

        Assert.True(await MutateSuccessAsync(client, first));
        Assert.True(await MutateSuccessAsync(client, second));
    }

    [Fact]
    public async Task RegisterDiagnosis_UnknownIcdCode_ReturnsSuccessFalse()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var success = await MutateSuccessAsync(
            client, DiagnosisServer.ValidRequest(icdCode: "ZZZ-UNKNOWN"));

        Assert.False(success);
    }

    [Fact]
    public async Task RegisterDiagnosis_MissingSymptomStart_ReturnsSuccessFalse()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest() with
        {
            SymptomStartDate = null,
            SymptomStartAge = null
        };

        var success = await MutateSuccessAsync(client, request);

        Assert.False(success);
    }

    [Fact]
    public async Task ChangeDiagnosisStatus_ActiveDiagnosis_ReturnsSuccessTrue()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var diagnosis = DiagnosisServer.DiagnosisFor(
            DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-STATUS");
        var repository = app.Services.GetRequiredService<InMemoryDiagnosisRepository>();
        repository.Seed(diagnosis);

        var response = await ChangeStatusAsync(
            client, diagnosis.SourceSystem, diagnosis.SourceRecordId);

        Assert.True(response.Success);
        Assert.Null(response.ErrorCode);
        Assert.Null(response.ErrorMessage);
        var updated = await repository.FindByIdAsync(diagnosis.Id);
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
        var repository = app.Services.GetRequiredService<InMemoryDiagnosisRepository>();
        repository.Seed(diagnosis);

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
}
