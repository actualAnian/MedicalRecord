using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Register.App.Endpoints;
using Register.App.ExternalAPI;
using Register.App.Services;
using Register.DataBase.Repositories;
using System.Text.Json.Serialization;

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
                sp.GetRequiredService<IICD10CatalogueService>()));
        services.AddScoped<IICD10CatalogueService, ICD10CatalogueService>();
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
            .AddMutationType<DiagnosisMutation>();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter()
            );
        });
        return services;
    }
    public static WebApplication MapApplication(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapControllers();

        app.MapDiagnosis();
        app.MapPatients();
        app.MapExternalRegistry();
        app.MapGraphQL("/graphql");

        return app;
    }
}
