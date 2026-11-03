using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Register.App.DTO;

namespace Register.IntegrationTests;

// Covers MapDiagnosis(): POST /diagnoses (OpenAPI / REST)
public class RestDiagnosisTests
{
    [Fact]
    public async Task MapDiagnosis_ValidRequest_ReturnsOk()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest();

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapDiagnosis_DuplicateSourceIdentity_ReturnsOk_Idempotent()
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
    }

    [Fact]
    public async Task MapDiagnosis_UnknownIcdCode_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var request = DiagnosisServer.ValidRequest(icdCode: "ZZZ-UNKNOWN");

        var response = await client.PostAsJsonAsync("/diagnoses", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    }

    [Fact]
    public async Task GetPatientDiagnoses_ValidStatus_ReturnsOnlyMatchingDiagnoses()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var repository = app.Services.GetRequiredService<InMemoryDiagnosisRepository>();
        repository.Seed(
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Active, "D-ACTIVE"),
            DiagnosisServer.DiagnosisFor(
                DiagnosisServer.ValidPatientId, ClinicalStatus.Healed, "D-HEALED"));

        var response = await client.GetAsync(
            $"/patients/{DiagnosisServer.ValidPatientId}/diagnoses?clinicalStatuses=Active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var diagnosis = Assert.Single(body.RootElement.EnumerateArray().ToList());
        Assert.Equal("D-ACTIVE", diagnosis.GetProperty("sourceRecordId").GetString());
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
        Assert.Contains("ValidationError", await response.Content.ReadAsStringAsync());
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
}
