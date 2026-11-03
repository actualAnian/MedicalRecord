using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Register.App.DTO;

namespace Register.App.ExternalAPI
{

    // mock registry, fails first two attempts, "succeeds" on the third attempt
    public static class ExternalRegistry
    {
        private static readonly Dictionary<string, int> _attempts = new();
        public static WebApplication MapExternalRegistry(this WebApplication app)
        {
            app.MapPost("/externalRequest", (Diagnosis diagnosis) =>
            {
                if (string.IsNullOrWhiteSpace(diagnosis.SourceRecordId))
                    return Results.BadRequest("SourceRecordId is required.");
                if (string.IsNullOrWhiteSpace(diagnosis.IcdCode?.Code))
                    return Results.BadRequest("IcdCode is required.");

                var diagnosisId = diagnosis.SourceRecordId;
                _attempts.TryGetValue(diagnosisId, out var attempts);
                attempts++;
                _attempts[diagnosisId] = attempts;

                // First two attempts fail temporarily.
                if (attempts <= 2)
                    return Results.Json(
                        new { error = $"Temporary failure. Attempt: {attempts}" },
                        statusCode: StatusCodes.Status503ServiceUnavailable);

                // Third and subsequent attempts succeed.
                return Results.Json(
                    new
                    {
                        diagnosisId,
                        attempts,
                        status = "accepted"
                    },
                    statusCode: StatusCodes.Status202Accepted);
            });
            return app;
        }
    }
}
