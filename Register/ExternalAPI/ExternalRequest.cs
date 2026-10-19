using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Register.DTO;

namespace Register.ExternalAPI
{
    // mock registry, fails first two attempts, "succeeds" on the third attempt
    public static class ExternalRegistry
    {
        private static readonly Dictionary<string, int> _attempts = new();
        public static WebApplication MapDiagnosis(this WebApplication app)
        {
            app.MapPost("/externalRequest", (Diagnosis diagnosis) =>
            {
                if (diagnosis.SourceRecordId is null)
                    return Results.BadRequest("DiagnosisId is required.");
                if (string.IsNullOrWhiteSpace(diagnosis.IcdCode.Code))
                    return Results.BadRequest("IcdCode is required.");

                var diagnosisId = diagnosis.SourceRecordId;
                _attempts.TryGetValue(diagnosisId, out var attempts);
                attempts++;
                _attempts[diagnosisId] = attempts;

                // First two attempts fail temporarily.
                if (attempts <= 2)
                    return Results.StatusCode(503);

                // Third and subsequent attempts succeed.
                return Results.Ok(
                    new
                    {
                        diagnosisId,
                        attempts,
                        status = "accepted"
                    });
            });
            return app;
        }
    }
}
