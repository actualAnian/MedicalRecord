using System;
using System.Collections.Generic;
using System.Text;

namespace Register.DTO
{
    public class Diagnosis
    {
        public string SourceSystem { get; set; }
        public string SourceRecordId { get; set; }
        public Patient Patient { get; set; }
        public ICD10 IcdCode { get; set; }
        public DateOnly DiagnosisDate { get; set; }
        public ClinicalStatus ClinicalStatus { get; set; }
        public ConfirmationStatus ConfirmationStatus { get; set; }
        [GraphQLIgnore]
        public SymptomStart SymptomStart { get; set; }

        public Diagnosis(
            string sourceSystem,
            string sourceRecordId,
            Patient patient,
            ICD10 icdCode,
            DateOnly diagnosisDate,
            ClinicalStatus clinicalStatus,
            ConfirmationStatus confirmationStatus,
            SymptomStart symptomStart)
        {
            SourceSystem = sourceSystem;
            SourceRecordId = sourceRecordId;
            Patient = patient;
            IcdCode = icdCode;
            DiagnosisDate = diagnosisDate;
            ClinicalStatus = clinicalStatus;
            ConfirmationStatus = confirmationStatus;
            SymptomStart = symptomStart;
        }
    }
}
