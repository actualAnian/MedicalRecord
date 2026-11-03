using Register.App.DTO;

namespace Register.DataBase.Repositories
{
    public interface IPatientRepository
    {
        Task<Patient?> FindPatientByPeselAsync(string pesel);
        Task<Patient?> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId);
    }
}