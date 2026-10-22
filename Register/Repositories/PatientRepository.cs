using Register.DTO;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Register.Repositories
{
    internal class PatientRepository : IPatientRepository
    {
        RegisterDBDapper _database;
        public PatientRepository(RegisterDBDapper database)
        {
            _database = database;
        }
        public Patient? FindPatientByPesel(string pesel)
        {
            var patient = _database.GetPatientByPesel(pesel);
            return patient;
        }
        public Patient? FindPatientBySourceIdentity(string sourceSystem, string sourceRecordId)
        {
            var patient = _database.GetPatientBySourceIdentity(sourceSystem, sourceRecordId);
            return patient;
        }
    }
}
