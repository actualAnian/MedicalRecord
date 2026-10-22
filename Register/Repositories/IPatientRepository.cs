using Register.DTO;

namespace Register.Repositories
{
    public interface IPatientRepository
    {
        Patient? FindPatientByPesel(string pesel);
        Patient? FindPatientBySourceIdentity(string sourceSystem, string sourceRecordId);
    }
}