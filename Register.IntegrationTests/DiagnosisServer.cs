using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Register.DTO;
using Register.Repositories;
using Register.Services;

namespace Register.IntegrationTests;

public static class DiagnosisServer
{
    public const string ValidCodingSystem = "http://hl7.org/fhir/sid/icd-10";
    public const string ValidIcdCode = "A00";
    public const string ValidPesel = "44051401359";
    public const string ValidSourceSystem = "HIS";
    public const string ValidPatientSourceRecordId = "PATIENT-1";
    public static readonly Guid ValidPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static RegisterDiagnosisRequest ValidRequest(
        string? sourceSystem = null,
        string? sourceRecordId = null,
        string icdCode = ValidIcdCode,
        DateOnly? symptomStartDate = null,
        int? symptomStartAge = null)
        => new(
            SourceSystem: sourceSystem ?? "HIS",
            SourceRecordId: sourceRecordId ?? Guid.NewGuid().ToString("N"),
            PatientPesel: "44051401359",
            MedicalRecordNumber: "MR-123",
            IcdCode: icdCode,
            CodingSystem: ValidCodingSystem,
            DiagnosisDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ClinicalStatus: "Active",
            ConfirmationStatus: "Confirmed",
            SymptomStartDate: symptomStartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            SymptomStartAge: symptomStartAge,
            VisitId: "VISIT-1");

    public static Diagnosis DiagnosisFor(
        Guid patientId,
        ClinicalStatus status,
        string? sourceRecordId = null,
        string sourceSystem = "HIS")
        => new(
            sourceSystem,
            sourceRecordId ?? Guid.NewGuid().ToString("N"),
            new Patient(patientId),
            new ICD10(ValidCodingSystem, ValidIcdCode, "Cholera"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            status,
            ConfirmationStatus.Confirmed,
            new SymptomStart.FromDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))));

    public static async Task<(WebApplication App, HttpClient Client)> CreateAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        var address = $"http://127.0.0.1:{GetFreeTcpPort()}";
        builder.WebHost.UseUrls(address);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // The service under test calls its own /externalRequest endpoint over HTTP.
        builder.Configuration["ExternalRegistry:BaseUrl"] = address;

        builder.Services.ConfigurationServices(builder.Configuration);

        // Replace real persistence with deterministic in-memory fake.
        var toRemove = builder.Services
            .Where(d => d.ServiceType == typeof(IDiagnosisRepository)
                     || d.ServiceType == typeof(DiagnosisRepository)
                     || d.ServiceType == typeof(IPatientRepository)
                     || d.ServiceType == typeof(RegisterDBDapper))
            .ToList();
        foreach (var d in toRemove) builder.Services.Remove(d);

        builder.Services.AddSingleton<InMemoryDiagnosisRepository>();
        builder.Services.AddScoped<IDiagnosisRepository>(sp =>
            sp.GetRequiredService<InMemoryDiagnosisRepository>());

        var patientRepository = new InMemoryPatientRepository();
        patientRepository.Seed(
            new Patient(ValidPatientId),
            ValidPesel,
            ValidSourceSystem,
            ValidPatientSourceRecordId);
        builder.Services.AddSingleton(patientRepository);
        builder.Services.AddScoped<IPatientRepository>(sp =>
            sp.GetRequiredService<InMemoryPatientRepository>());

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.MapApplication();
        await app.StartAsync();

        var client = new HttpClient { BaseAddress = new Uri(address) };
        return (app, client);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
