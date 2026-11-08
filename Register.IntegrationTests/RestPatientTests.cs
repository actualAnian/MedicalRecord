using System.Net;
using System.Text.Json;

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
    public async Task GetPatient_UnknownPesel_ReturnsForbiddenWithNotFoundOnLookup()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync("/patients?pesel=00000000000");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = Assert.Single(body.RootElement.GetProperty("errors").EnumerateArray().ToList());
        Assert.Equal("NotFoundOnLookup", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetPatient_UnknownSourceIdentity_ReturnsForbiddenWithNotFoundOnLookup()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync(
            "/patients?sourceSystem=UNKNOWN&sourceRecordId=UNKNOWN");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = Assert.Single(body.RootElement.GetProperty("errors").EnumerateArray().ToList());
        Assert.Equal("NotFoundOnLookup", error.GetProperty("code").GetString());
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

    [Theory]
    [InlineData("?sourceSystem=HIS")]
    [InlineData("?sourceRecordId=PATIENT-1")]
    public async Task GetPatient_WithIncompleteSourceIdentity_ReturnsBadRequest(string query)
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var response = await client.GetAsync($"/patients{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPatient_WithPeselAndSourceIdentity_UsesPesel()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        await DiagnosisServer.AddPatientAsync(
            Guid.NewGuid(),
            "00000000000",
            ("OTHER", "OTHER-1"));

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
