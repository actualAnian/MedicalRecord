using Dapper;
using Microsoft.Data.SqlClient;
using Register.App;
using Register.App.DTO;
using Register.DataBase.DTO;
using Register.DataBase.Mappers;

namespace Register.DataBase.Repositories
{
    public class DiagnosisRepository : IDiagnosisRepository
    {
        private readonly string _connectionString;
        private readonly IICD10Catalogue _iCD10Catalogue;

        public DiagnosisRepository(string connectionString, IICD10Catalogue iCD10Catalogue)
        {
            _connectionString = connectionString;
            _iCD10Catalogue = iCD10Catalogue;
        }
        public async Task AddAsync(Diagnosis diagnosis)
        {
            var diagnosisDB = DiagnosisMapper.ToDatabase(diagnosis);
            const string sql = """
            INSERT INTO dbo.Diagnoses
            (
                Id,
                SourceSystem,
                SourceRecordId,
                PatientId,
                IcdSystem,
                IcdCode,
                DiagnosisDate,
                ClinicalStatus,
                ConfirmationStatus,
                SymptomStartType,
                SymptomStartDate,
                SymptomStartAge
            )
            VALUES
            (
                @Id,
                @SourceSystem,
                @SourceRecordId,
                @PatientId,
                @IcdSystem,
                @IcdCode,
                @DiagnosisDate,
                @ClinicalStatus,
                @ConfirmationStatus,
                @SymptomStartType,
                @SymptomStartDate,
                @SymptomStartAge
            );
            """;
            await using var connection =
                new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, diagnosisDB);
        }

        public async Task<IEnumerable<Diagnosis>> FindByStatusAsync(Guid patientId, ClinicalStatus status)
        {
            const string sql = """
                SELECT
                    Id,
                    SourceSystem,
                    SourceRecordId,
                    PatientId,
                    IcdSystem,
                    IcdCode,
                    DiagnosisDate,
                    ClinicalStatus,
                    ConfirmationStatus,
                    SymptomStartType,
                    SymptomStartDate,
                    SymptomStartAge
                FROM dbo.Diagnoses
                WHERE PatientId = @PatientId
                  AND ClinicalStatus = @Status
                ORDER BY DiagnosisDate DESC;
                """;

            await using var connection = new SqlConnection(_connectionString);

            var dbDiagnoses = await connection.QueryAsync<DiagnosisDb>(
                sql,
                new
                {
                    PatientId = patientId,
                    Status = status
                });

            return dbDiagnoses
                .Select(diagnosis => DiagnosisMapper.ToDomain(diagnosis, _iCD10Catalogue))
                .ToList();
        }

        public async Task<Diagnosis?> FindBySourceIdentityAsync(
            string sourceSystem,
            string sourceRecordId)
        {
            const string sql = """
                SELECT
                    Id,
                    SourceSystem,
                    SourceRecordId,
                    PatientId,
                    IcdSystem,
                    IcdCode,
                    DiagnosisDate,
                    ClinicalStatus,
                    ConfirmationStatus,
                    SymptomStartType,
                    SymptomStartDate,
                    SymptomStartAge
                FROM dbo.Diagnoses
                WHERE SourceSystem = @SourceSystem
                  AND SourceRecordId = @SourceRecordId;
                """;

            await using var connection = new SqlConnection(_connectionString);

            var dbDiagnosis = await connection.QuerySingleOrDefaultAsync<DiagnosisDb>(
                sql,
                new
                {
                    SourceSystem = sourceSystem,
                    SourceRecordId = sourceRecordId
                });

            return dbDiagnosis is null
                ? null
                : DiagnosisMapper.ToDomain(dbDiagnosis, _iCD10Catalogue);
        }
    }
}