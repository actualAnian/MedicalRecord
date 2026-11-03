using Register.App;
using Register.App.DTO;
using Register.DataBase.Repositories;

namespace Register.App.Services
{
    public class PatientService(IPatientRepository repository) : IPatientService
    {

        public async Task<Result<Patient>> FindPatientByPeselAsync(string pesel)
        {
            var patient = await repository.FindPatientByPeselAsync(pesel);
            if (patient is null) return Result<Patient>.Fail([new Error.NotFoundOnLookup("Patient", $"Patient with PESEL {pesel} not found")]);
            return Result<Patient>.Success(patient);
        }

        public async Task<Result<Patient>> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        {
            var patient = await repository.FindPatientBySourceIdentityAsync(sourceSystem, sourceRecordId);
            if (patient is null) return Result<Patient>.Fail([new Error.NotFoundOnLookup("Patient", $"Patient with source identity ({sourceSystem}, {sourceRecordId}) not found")]);
            return Result<Patient>.Success(patient);
        }
    }
}