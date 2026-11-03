using Register.App;
using Register.App.DTO;

namespace Register.App.Services
{
    public interface IPatientService
    {
        Task<Result<Patient>> FindPatientByPeselAsync(string pesel);
        Task<Result<Patient>> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId);
    }
}