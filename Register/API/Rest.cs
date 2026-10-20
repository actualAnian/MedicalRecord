using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Register;
using Register.ExternalAPI;

namespace Register.API;

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
        return app;
    }
}