using Dapper;
using Microsoft.Data.SqlClient;
using Register.App.DTO;
using Register.DataBase.DTO;

namespace Register.DataBase.Repositories
{
    internal class PatientRepository : IPatientRepository
    {
        private readonly string _connectionString;

        public PatientRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Patient?> FindPatientByPeselAsync(string pesel)
        {
            const string sql = """
                SELECT Id, PESEL
                FROM dbo.Patients
                WHERE PESEL = @Pesel;
                """;

            await using var connection = new SqlConnection(_connectionString);

            var patient = await connection.QuerySingleOrDefaultAsync<PatientDb>(
                sql,
                new { PESEL = pesel });

            return patient is null
                ? null
                : new Patient(patient.Id, patient.PESEL);
        }

        public async Task<Patient?> FindPatientBySourceIdentityAsync(string sourceSystem, string sourceRecordId)
        {
            const string sql = """
                SELECT p.Id, p.PESEL
                FROM dbo.PatientExternalIdentifiers i
                INNER JOIN dbo.Patients p ON p.Id = i.PatientId
                WHERE i.SourceSystem = @SourceSystem
                  AND i.SourceRecordId = @SourceRecordId;
                """;

            await using var connection = new SqlConnection(_connectionString);

            var patient = await connection.QuerySingleOrDefaultAsync<PatientDb>(
                sql,
                new
                {
                    SourceSystem = sourceSystem,
                    SourceRecordId = sourceRecordId
                });

            return patient is null
                ? null
                : new Patient(patient.Id, patient.PESEL);
        }
    }
}
