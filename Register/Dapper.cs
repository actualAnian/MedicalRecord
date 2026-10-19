using Dapper;
using Microsoft.Data.Sqlite;
using Register.DTO;

namespace Register
{
    public class RegisterDBDapper(string connectionString)
    {
        public async Task AddDiagnosisAsync(Diagnosis diagnosis)
        {
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var sql = @"
                INSERT INTO Diagnosis (SourceSystem, SourceRecordId, IcdCode, DiagnosisDate, ClinicalStatus, ConfirmationStatus, SymptomStart)
                VALUES ($sourceSystem, $SourceRecordId, $icdCode, $diagnosisDate, $clinicalStatus, $confirmationStatus, $symptomStart);
            ";
            await connection.ExecuteAsync(sql, diagnosis);
        }
        public async Task<IReadOnlyList<Diagnosis>> GetDiagnosisAsync(Guid patientId, ClinicalStatus status)
        {
            // sortowanie tutaj
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var sql = @"
            SELECT Id, FirstName, LastName, BirthDate
            FROM Patients;
            ";
            var diagnosis = connection.Query<Diagnosis>(sql).ToList();
            return diagnosis;
        }
    }
}
