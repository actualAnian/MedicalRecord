using Register.DTO;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Register.Repositories
{
    public interface IDiagnosisRepository
    {
        IEnumerable<Diagnosis> FindByStatus(Guid patientId, ClinicalStatus status);
        Diagnosis? FindBySourceIdentity(string sourceSystem, string sourceRecordId);
        void Add(Diagnosis diagnosis);
        string? GetDescriptionForCode(string system, string code);
    }

    public class DiagnosisRepository : IDiagnosisRepository
    {
        private readonly List<Diagnosis> _diagnoses = [];
        RegisterDBDapper _database;
        private readonly List<ICD10> _icd10Codes;

        public DiagnosisRepository(RegisterDBDapper database)
        {
            _database = database;
            _icd10Codes = ICD10Reader.Read("icd10Codes.json");
        }
        public Diagnosis? FindBySourceIdentity(
            string sourceSystem,
            string sourceRecordId)
        {
            var diagnosis = _diagnoses.FirstOrDefault(x =>
                x.SourceSystem == sourceSystem &&
                x.SourceRecordId == sourceRecordId);
            return diagnosis;
        }

        public void Add(Diagnosis diagnosis)
        {
            _diagnoses.Add(diagnosis);
        }
        public string? GetDescriptionForCode(string system, string code)
        {
            return _icd10Codes.FirstOrDefault(c => c.System == system && c.Code == code)?.Display;
        }

        public IEnumerable<Diagnosis> FindByStatus(Guid patientId, ClinicalStatus status)
        {
            return _diagnoses
                .Where(d => d.Patient.Id == patientId && d.ClinicalStatus == status)
                .ToList();
        }
    }
}