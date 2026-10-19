using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Register.API;

namespace Register;

public static class ProgramExtensions
{
    public static IServiceCollection ConfigurationServices(this IServiceCollection services)
    {
        services.AddSingleton(new RegisterDBDapper("Data Source=:memory:"));
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        services.AddScoped<DiagnosisRepository>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddControllers();
        services.AddOpenApi();
        services.AddGraphQLServer()
            .AddQueryType<DiagnosisQueries>()
            .AddMutationType<DiagnosisMutation>();
        return services;
    }
    public static WebApplication MapApplication(this WebApplication app)
    {
        app.MapOpenApi();
        //app.MapScalarApiReference();
        app.MapControllers();

        //app.MapPatientApi();
        app.MapDiagnosis();
        app.MapGraphQL("/graphql");

        return app;
    }
}
