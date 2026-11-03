using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Register.App;
using Register.App.DTO;
using Register.App.Services;
using Register.DataBase.Repositories;

namespace Register.IntegrationTests;

public static class DiagnosisServer
{
    public const string ValidCodingSystem = "http://hl7.org/fhir/sid/icd-10";
    public const string ValidIcdCode = "A00";
    public const string ValidPesel = "44051401359";
    public const string ValidSourceSystem = "HIS";
    public const string ValidPatientSourceRecordId = "PATIENT-1";
    public const string ValidMedicalRecordNumber = "MR-123";
    public static readonly Guid ValidPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static RegisterDiagnosisRequest ValidRequest(
        string? sourceSystem = null,
        string? sourceRecordId = null,
        string icdCode = ValidIcdCode,
        DateOnly? symptomStartDate = null,
        int? symptomStartAge = null,
        string? patientPesel = ValidPesel,
        string? medicalRecordNumber = ValidMedicalRecordNumber)
        => new(
            SourceSystem: sourceSystem ?? "HIS",
            SourceRecordId: sourceRecordId ?? Guid.NewGuid().ToString("N"),
            PatientPesel: patientPesel,
            MedicalRecordNumber: medicalRecordNumber,
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
            Guid.NewGuid(),
            sourceSystem,
            sourceRecordId ?? Guid.NewGuid().ToString("N"),
            patientId,
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

        // Required by ConfigurationServices; persistence is replaced with in-memory fakes below.
        builder.Configuration["ConnectionStrings:DefaultConnection"] =
            "Server=localhost,1433;Database=MedicalRegister;User Id=registerUser;Password=unused;TrustServerCertificate=True;";
        // The service under test calls its own /externalRequest endpoint over HTTP.
        builder.Configuration["ExternalRegistry:BaseUrl"] = address;

        builder.Services.ConfigurationServices(builder.Configuration);

        // Replace real persistence and the file-backed catalogue with deterministic fakes.
        var toRemove = builder.Services
            .Where(d => d.ServiceType == typeof(IDiagnosisRepository)
                     || d.ServiceType == typeof(DiagnosisRepository)
                     || d.ServiceType == typeof(IPatientRepository)
                     || d.ServiceType == typeof(IICD10Catalogue))
            .ToList();
        foreach (var d in toRemove) builder.Services.Remove(d);

        builder.Services.AddSingleton<InMemoryDiagnosisRepository>();
        builder.Services.AddScoped<IDiagnosisRepository>(sp =>
            sp.GetRequiredService<InMemoryDiagnosisRepository>());
        builder.Services.AddSingleton<IICD10Catalogue, TestIcd10Catalogue>();

        var patientRepository = new InMemoryPatientRepository();
        patientRepository.Seed(
            new Patient(ValidPatientId, ValidPesel),
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

public sealed class TestIcd10Catalogue : IICD10Catalogue
{
    private static readonly Dictionary<(string System, string Code), string> KnownCodes = new()
    {
        [("http://hl7.org/fhir/sid/icd-10", "A00")] = "Cholera",
        [("http://hl7.org/fhir/sid/icd-10", "A00.1")] = "Typhoid and paratyphoid fevers",
        [("http://hl7.org/fhir/sid/icd-10", "A02.0")] = "Salmonella enteritis",
        [("http://hl7.org/fhir/sid/icd-10", "A03")] = "Shigellosis",
        [("http://hl7.org/fhir/sid/icd-10", "A04")] = "Other bacterial intestinal infections",
    };

    public ICD10? Find(string system, string code)
        => KnownCodes.TryGetValue((system, code), out var display)
            ? new ICD10(system, code, display)
            : null;

    public string? GetDescriptionForCode(string system, string code)
        => KnownCodes.TryGetValue((system, code), out var display) ? display : null;
}
