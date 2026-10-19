using Register.DTO;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Register
{
    public interface IDiagnosisRepository
    {
        Result<Diagnosis> FindBySourceIdentity(string sourceSystem, string sourceRecordId);
        void Add(Diagnosis diagnosis);
        public Patient GetPatient(string system, string patientCode);
        string? GetDescriptionForCode(string system, string code);
    }

    public class DiagnosisRepository : IDiagnosisRepository
    {
        private readonly List<Patient> _patients;
        private readonly List<Diagnosis> _diagnoses = [];
        RegisterDBDapper _database;
        private readonly List<ICD10> _icd10Codes;

        public DiagnosisRepository(RegisterDBDapper database)
        {
            _database = database;
            _icd10Codes = ICD10Reader.Read("icd10Codes.json");
        }

        public Patient GetPatient(string system, string patientCode)
        {
            return new(new());
        }

        public Result<Diagnosis> FindBySourceIdentity(
            string sourceSystem,
            string sourceRecordId)
        {
            var diagnosis = _diagnoses.FirstOrDefault(x =>
                x.SourceSystem == sourceSystem &&
                x.SourceRecordId == sourceRecordId);
            return diagnosis is not null ? Result<Diagnosis>.Success(diagnosis) : Result<Diagnosis>.Fail([new Error.NotFound("Diagnosis", Guid.Empty)]);
        }

        public void Add(Diagnosis diagnosis)
        {
            _diagnoses.Add(diagnosis);
        }
        public Task<IReadOnlyList<Diagnosis>> GetPatientDiagnoses(Guid patientId, ClinicalStatus status) 
        {
            var diagnoses = _database.GetDiagnosisAsync(patientId, status);
            return diagnoses;
        }

        public string? GetDescriptionForCode(string system, string code)
        {
            return _icd10Codes.FirstOrDefault(c => c.System == system && c.Code == code)?.Display;
        }
    }
}