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
                return result.ToHttpResult();
            });


        app.MapPost("/diagnoses/status", async (ChangeDiagnosisStatusRequest request, [Service] IDiagnosisService service) =>
            {
                var result = await service.ChangeStatus(request);
                return result.ToHttpResult();
            });


        app.MapGet("/patients/{patientId}/diagnoses", async (Guid patientId, string clinicalStatuses, [Service] IDiagnosisService service) =>
            {
                var result = await service.FindDiagnosesByStatus(patientId, clinicalStatuses);
                return result.ToHttpResult();
            });

        app.MapGet("/patients", async (string? pesel, string? sourceSystem, string? sourceRecordId, [Service] IPatientService service) =>
            {
                if (!string.IsNullOrWhiteSpace(pesel))
                {
                    var result = await service.FindPatientByPeselAsync(pesel);
                    return result.ToHttpResult();
                }
                if (!string.IsNullOrWhiteSpace(sourceSystem) && !string.IsNullOrWhiteSpace(sourceRecordId))
                {
                    var result = await service.FindPatientBySourceIdentityAsync(sourceSystem, sourceRecordId);
                    return result.ToHttpResult();
                }
                return Results.BadRequest("Either pesel or sourceSystem and sourceRecordId must be provided.");
            });
        return app;
    }
}