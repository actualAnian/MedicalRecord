using System.Net;
using System.Net.Sockets;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
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

    private const string DefaultConnectionString =
        "Server=localhost,1433;Database=MedicalRegister;User Id=registerUser;Password=UserPassword!;TrustServerCertificate=True;";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("MEDICAL_REGISTER_TEST_CONNECTION")
        ?? DefaultConnectionString;

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
        string sourceSystem = "HIS",
        string icdSystem = ValidCodingSystem,
        string icdCode = ValidIcdCode)
        => new(
            Guid.NewGuid(),
            sourceSystem,
            sourceRecordId ?? Guid.NewGuid().ToString("N"),
            patientId,
            new ICD10(icdSystem, icdCode, "Cholera"),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            status,
            ConfirmationStatus.Confirmed,
            new SymptomStart.FromDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))));

    public static async Task<(WebApplication App, HttpClient Client)> CreateAsync()
    {
        await ResetDatabaseAsync();
        await AddPatientAsync(
            ValidPatientId,
            ValidPesel,
            (ValidSourceSystem, ValidPatientSourceRecordId),
            (ValidSourceSystem, ValidMedicalRecordNumber));

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        var address = $"http://127.0.0.1:{GetFreeTcpPort()}";
        builder.WebHost.UseUrls(address);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Configuration["ConnectionStrings:DefaultConnection"] = ConnectionString;
        builder.Configuration["ExternalRegistry:BaseUrl"] = address;

        builder.Services.ConfigurationServices(builder.Configuration);

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.MapApplication();
        await app.StartAsync();

        var client = new HttpClient { BaseAddress = new Uri(address) };
        return (app, client);
    }

    public static async Task ResetDatabaseAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);

        try
        {
            await connection.OpenAsync();
        }
        catch (SqlException exception)
        {
            throw new InvalidOperationException(
                "Integration tests require the SQL Server database to be running. " +
                "Start it with 'docker compose up -d' and verify the connection string.",
                exception);
        }

        await connection.ExecuteAsync("""
            DELETE FROM dbo.Diagnoses;
            DELETE FROM dbo.PatientExternalIdentifiers;
            DELETE FROM dbo.Patients;
            """);
    }

    public static async Task AddPatientAsync(
        Guid patientId,
        string pesel,
        params (string SourceSystem, string SourceRecordId)[] identifiers)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            "INSERT INTO dbo.Patients (Id, PESEL) VALUES (@PatientId, @Pesel);",
            new { PatientId = patientId, Pesel = pesel });

        foreach (var identifier in identifiers)
        {
            await connection.ExecuteAsync("""
                INSERT INTO dbo.PatientExternalIdentifiers
                    (SourceSystem, SourceRecordId, PatientId)
                VALUES
                    (@SourceSystem, @SourceRecordId, @PatientId);
                """,
                new
                {
                    identifier.SourceSystem,
                    identifier.SourceRecordId,
                    PatientId = patientId
                });
        }
    }

    public static async Task AddDiagnosesAsync(
        WebApplication app,
        params Diagnosis[] diagnoses)
    {
        using var scope = app.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();

        foreach (var diagnosis in diagnoses)
            await repository.AddAsync(diagnosis);
    }

    public static async Task<Diagnosis?> FindDiagnosisAsync(
        WebApplication app,
        Guid diagnosisId)
    {
        using var scope = app.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();
        return await repository.FindByIdAsync(diagnosisId);
    }

    public static async Task<Diagnosis?> FindDiagnosisAsync(
        WebApplication app,
        string sourceSystem,
        string sourceRecordId)
    {
        using var scope = app.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDiagnosisRepository>();
        return await repository.FindBySourceIdentityAsync(sourceSystem, sourceRecordId);
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
