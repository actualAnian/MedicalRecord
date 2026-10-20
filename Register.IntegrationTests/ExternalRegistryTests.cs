using System.Net;
using System.Net.Http.Json;

namespace Register.IntegrationTests;

// Covers MapExternalRegistry(): POST /externalRequest (mock external registry)
public class ExternalRegistryTests
{
    [Fact]
    public async Task MapExternalRegistry_FailsTwiceThenAccepts()
    {
        var (app, client) = await DiagnosisServer.CreateAsync();
        await using var _ = app;
        using var __ = client;

        var payload = new
        {
            sourceRecordId = Guid.NewGuid().ToString("N"),
            icdCode = new { code = "A00" }
        };

        var first = await client.PostAsJsonAsync("/externalRequest", payload);
        var second = await client.PostAsJsonAsync("/externalRequest", payload);
        var third = await client.PostAsJsonAsync("/externalRequest", payload);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, first.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, second.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, third.StatusCode);
    }
}
