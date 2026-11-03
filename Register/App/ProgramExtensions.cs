using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Register.App.Endpoints;
using Register.App.ExternalAPI;
using Register.App.Services;
using Register.DataBase.Repositories;

namespace Register.App;

public static class ProgramExtensions
{
    public static IServiceCollection ConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found.");

        services.AddScoped<IDiagnosisRepository>(sp =>
            new DiagnosisRepository(
                connectionString,
                sp.GetRequiredService<IICD10Catalogue>()));
        services.AddSingleton<IICD10Catalogue, ICD10Catalogue>();
        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository>(_ => new PatientRepository(connectionString));
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
