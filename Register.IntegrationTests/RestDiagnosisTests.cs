using System.Net;
using System.Net.Http.Json;

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
}
