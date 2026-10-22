using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Register.API;
using Register.ExternalAPI;
using Register.Repositories;
using Register.Services;

namespace Register;

public static class ProgramExtensions
{
    public static IServiceCollection ConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new RegisterDBDapper("Data Source=:memory:"));
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        services.AddScoped<DiagnosisRepository>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientService, PatientService>();

        var externalRegistryUrl = configuration["ExternalRegistry:BaseUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<IExternalRegistryClient, ExternalRegistryClient>(
            http => http.BaseAddress = new Uri(externalRegistryUrl));
        services.AddControllers();
        services.AddOpenApi();
        services.AddGraphQLServer()
            .AddQueryType<DiagnosisQueries>()
            .AddMutationType<DiagnosisMutation>()
            .AllowIntrospection(true);
        return services;
    }
    public static WebApplication MapApplication(this WebApplication app)
    {
        app.MapOpenApi();
        //app.MapScalarApiReference();
        app.MapControllers();

        //app.MapPatientApi();
        app.MapDiagnosis();
        app.MapExternalRegistry();
        app.MapGraphQL("/graphql");

        return app;
    }
}
