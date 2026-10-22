using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Register.DTO;

namespace Register.IntegrationTests;

// Covers GET /patients (OpenAPI / REST)
public class RestPatientTests
{
    [Fact]
    public async Task GetPatient_ByPesel_ReturnsOkWithPatient()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync($"/patients?pesel={DiagnosisServer.ValidPesel}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            DiagnosisServer.ValidPatientId,
            body.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetPatient_BySourceIdentity_ReturnsOkWithPatient()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            $"/patients?sourceSystem={DiagnosisServer.ValidSourceSystem}" +
            $"&sourceRecordId={DiagnosisServer.ValidPatientSourceRecordId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            DiagnosisServer.ValidPatientId,
            body.RootElement.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetPatient_UnknownPesel_ReturnsBadRequestWithNotFoundOnLookup()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync("/patients?pesel=00000000000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("NotFoundOnLookup", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetPatient_UnknownSourceIdentity_ReturnsBadRequestWithNotFoundOnLookup()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            "/patients?sourceSystem=UNKNOWN&sourceRecordId=UNKNOWN");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("NotFoundOnLookup", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetPatient_WithoutLookupParameters_ReturnsBadRequest()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync("/patients");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPatient_WithPeselAndSourceIdentity_UsesPesel()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var sourcePatientId = Guid.NewGuid();
        var patientRepository = app.Services.GetRequiredService<InMemoryPatientRepository>();
        patientRepository.Seed(
            new Patient(sourcePatientId), "00000000000", "OTHER", "OTHER-1");

        var response = await client.GetAsync(
            $"/patients?pesel={DiagnosisServer.ValidPesel}" +
            "&sourceSystem=OTHER&sourceRecordId=OTHER-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            DiagnosisServer.ValidPatientId,
            body.RootElement.GetProperty("id").GetGuid());
    }
}
