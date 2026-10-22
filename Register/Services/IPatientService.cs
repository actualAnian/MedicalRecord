using Register.DTO;
using Register.Repositories;

namespace Register.Services
{
    public interface IPatientService
    {
        Result<Patient> FindPatientByPesel(string pesel);
        Result<Patient> FindPatientBySourceIdentity(string sourceSystem, string sourceRecordId);
    }
    public class PatientService(IPatientRepository repository) : IPatientService
    {
        public Result<Patient> FindPatientByPesel(string pesel)
        {
            var patient = repository.FindPatientByPesel(pesel);
            if (patient is null) return Result<Patient>.Fail([new Error.NotFoundOnLookup("Patient", $"Patient with PESEL {pesel} not found")]);
            return Result<Patient>.Success(patient);
        }
        public Result<Patient> FindPatientBySourceIdentity(string sourceSystem, string sourceRecordId)
        {
            var patient = repository.FindPatientBySourceIdentity(sourceSystem, sourceRecordId);
            if (patient is null) return Result<Patient>.Fail([new Error.NotFoundOnLookup("Patient", $"Patient with source identity ({sourceSystem}, {sourceRecordId}) not found")]);
            return Result<Patient>.Success(patient);
        }
    }
}