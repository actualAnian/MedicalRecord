using Register.DTO;
using Register.Repositories;
using Register.Services;

namespace Register.UnitTests;

public class PatientServiceTests
{
    [Fact]
    public void FindPatientByPesel_ExistingPatient_ReturnsSuccess()
    {
        var patient = new Patient(Guid.NewGuid());
        var repository = new FakePatientRepository { PatientByPesel = patient };
        var service = new PatientService(repository);

        var result = service.FindPatientByPesel("44051401359");

        Assert.True(result.IsSuccess);
        Assert.Same(patient, result.Value);
        Assert.Equal("44051401359", repository.LastPesel);
    }

    [Fact]
    public void FindPatientByPesel_MissingPatient_ReturnsNotFoundOnLookup()
    {
        var service = new PatientService(new FakePatientRepository());

        var result = service.FindPatientByPesel("44051401359");

        Assert.True(result.IsFailure);
        var error = Assert.IsType<Error.NotFoundOnLookup>(Assert.Single(result.Errors));
        Assert.Equal("NotFoundOnLookup", error.Code);
        Assert.Contains("44051401359", error.Message);
    }

    [Fact]
    public void FindPatientBySourceIdentity_ExistingPatient_ReturnsSuccess()
    {
        var patient = new Patient(Guid.NewGuid());
        var repository = new FakePatientRepository { PatientBySourceIdentity = patient };
        var service = new PatientService(repository);

        var result = service.FindPatientBySourceIdentity("HIS", "REC-1");

        Assert.True(result.IsSuccess);
        Assert.Same(patient, result.Value);
        Assert.Equal("HIS", repository.LastSourceSystem);
        Assert.Equal("REC-1", repository.LastSourceRecordId);
    }

    [Fact]
    public void FindPatientBySourceIdentity_MissingPatient_ReturnsNotFoundOnLookup()
    {
        var service = new PatientService(new FakePatientRepository());

        var result = service.FindPatientBySourceIdentity("HIS", "REC-1");

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

        public Patient? FindPatientByPesel(string pesel)
        {
            LastPesel = pesel;
            return PatientByPesel;
        }

        public Patient? FindPatientBySourceIdentity(string sourceSystem, string sourceRecordId)
        {
            LastSourceSystem = sourceSystem;
            LastSourceRecordId = sourceRecordId;
            return PatientBySourceIdentity;
        }
    }
}
