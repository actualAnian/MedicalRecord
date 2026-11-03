using Register.App;
using Register.App.DTO;
using Register.App.Services;
using Register.DataBase.Repositories;

namespace Register.UnitTests;

public class PatientServiceTests
{
    [Fact]
    public async Task FindPatientByPeselAsync_ExistingPatient_ReturnsSuccess()
    {
        var patient = new Patient(Guid.NewGuid(), "44051401359");
        var repository = new FakePatientRepository { PatientByPesel = patient };
        var service = new PatientService(repository);

        var result = await service.FindPatientByPeselAsync("44051401359");

        Assert.True(result.IsSuccess);
        Assert.Same(patient, result.Value);
        Assert.Equal("44051401359", repository.LastPesel);
    }

    [Fact]
    public async Task FindPatientByPeselAsync_MissingPatient_ReturnsNotFoundOnLookup()
    {
        var service = new PatientService(new FakePatientRepository());

        var result = await service.FindPatientByPeselAsync("44051401359");

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.NotFoundOnLookup>(Assert.Single(result.Errors));
        Assert.Equal("NotFoundOnLookup", error.Code);
        Assert.Contains("44051401359", error.Message);
    }

    [Fact]
    public async Task FindPatientBySourceIdentityAsync_ExistingPatient_ReturnsSuccess()
    {
        var patient = new Patient(Guid.NewGuid(), "44051401359");
        var repository = new FakePatientRepository { PatientBySourceIdentity = patient };
        var service = new PatientService(repository);

        var result = await service.FindPatientBySourceIdentityAsync("HIS", "REC-1");

        Assert.True(result.IsSuccess);
        Assert.Same(patient, result.Value);
        Assert.Equal("HIS", repository.LastSourceSystem);
        Assert.Equal("REC-1", repository.LastSourceRecordId);
    }

    [Fact]
    public async Task FindPatientBySourceIdentityAsync_MissingPatient_ReturnsNotFoundOnLookup()
    {
        var service = new PatientService(new FakePatientRepository());

        var result = await service.FindPatientBySourceIdentityAsync("HIS", "REC-1");

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.NotFoundOnLookup>(Assert.Single(result.Errors));
        Assert.Equal("NotFoundOnLookup", error.Code);
        Assert.Contains("HIS", error.Message);
        Assert.Contains("REC-1", error.Message);
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Patient? PatientByPesel { get; init; }
        public Patient? PatientBySourceIdentity { get; init; }
        public string? LastPesel { get; private set; }
        public string? LastSourceSystem { get; private set; }
        public string? LastSourceRecordId { get; private set; }

        public Task<Patient?> FindPatientByPeselAsync(string pesel)
        {
            LastPesel = pesel;
            return Task.FromResult(PatientByPesel);
        }

        public Task<Patient?> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        {
            LastSourceSystem = sourceSystem;
            LastSourceRecordId = sourceRecordId;
            return Task.FromResult(PatientBySourceIdentity);
        }
    }
}
