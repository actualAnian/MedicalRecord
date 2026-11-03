using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Register.App;
using Register.App.DTO;

namespace Register.App.ExternalAPI
{
    public interface IExternalRegistryClient
    {
        Task<Result<Diagnosis>> RegisterAsync(Diagnosis diagnosis, CancellationToken cancellationToken = default);
    }

    public sealed class ExternalRegistryClient : IExternalRegistryClient
    {
        private readonly HttpClient _httpClient;

        public ExternalRegistryClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Result<Diagnosis>> RegisterAsync(
            Diagnosis diagnosis,
            CancellationToken cancellationToken = default)
        {
            var response = await Retry.ExecuteAsync(
                async () =>
                {
                    using var message = await _httpClient.PostAsJsonAsync(
                        "/externalRequest", diagnosis, cancellationToken);
                    var body = await message.Content.ReadAsStringAsync(cancellationToken);
                    return new ExternalResponse((int)message.StatusCode, body);
                },
                r => r.StatusCode == StatusCodes.Status503ServiceUnavailable,
                cancellationToken);

            return response.StatusCode == StatusCodes.Status202Accepted
                ? Result<Diagnosis>.Success(diagnosis)
                : Result<Diagnosis>.Fail(
                    [new Error.External(
                        $"External registry returned {response.StatusCode}: {response.Body}")]);
        }

        private readonly record struct ExternalResponse(int StatusCode, string Body);
    }
}
