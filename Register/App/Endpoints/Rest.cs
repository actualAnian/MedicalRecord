using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Register.App.Services;

namespace Register.App.Endpoints;

public static class RestRegister
{
    public static WebApplication MapDiagnosis(this WebApplication app)
    {
        app.MapPost("/diagnoses", async (RegisterDiagnosisRequest request, [Service] IDiagnosisService service) =>
            {
                var result = await service.Register(request);

                if (result.IsSuccess) return Results.Ok("created");
                var error = result.Errors;
                return Results.BadRequest(error);
            });


        app.MapGet("/patients/{patientId}/diagnoses", async (Guid patientId, string clinicalStatuses, [Service] IDiagnosisService service) =>
            {
                var result = await service.FindDiagnosesByStatus(patientId, clinicalStatuses);
                if (result.IsSuccess) return Results.Ok(result.Value);
                var error = result.Errors;
                return Results.BadRequest(error);
            });

        app.MapGet("/patients", async (string? pesel, string? sourceSystem, string? sourceRecordId, [Service] IPatientService service) =>
            {
                if (!string.IsNullOrWhiteSpace(pesel))
                {
                    var result = await service.FindPatientByPeselAsync(pesel);
                    if (result.IsSuccess) return Results.Ok(result.Value);
                    var error = result.Errors;
                    return Results.BadRequest(error);
                }
                if (!string.IsNullOrWhiteSpace(sourceSystem) && !string.IsNullOrWhiteSpace(sourceRecordId))
                {
                    var result = await service.FindPatientBySourceIdentityAsync(sourceSystem, sourceRecordId);
                    if (result.IsSuccess) return Results.Ok(result.Value);
                    var error = result.Errors;
                    return Results.BadRequest(error);
                }
                return Results.BadRequest("Either pesel or sourceSystem and sourceRecordId must be provided.");
            });
        return app;
    }
}