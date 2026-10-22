using Register.Services;
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
}
