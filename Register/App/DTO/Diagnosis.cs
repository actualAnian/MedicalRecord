namespace Register.App.DTO
{
    public class Diagnosis(
        Guid id,
        string sourceSystem,
        string sourceRecordId,
        Guid patientId,
        ICD10 icdCode,
        DateOnly diagnosisDate,
        ClinicalStatus clinicalStatus,
        ConfirmationStatus confirmationStatus,
        SymptomStart symptomStart)
    {
        public Guid Id { get; set; } = id;
        public string SourceSystem { get; set; } = sourceSystem;
        public string SourceRecordId { get; set; } = sourceRecordId;
        public Guid PatientId { get; set; } = patientId;
        public ICD10 IcdCode { get; set; } = icdCode;
        public DateOnly DiagnosisDate { get; set; } = diagnosisDate;
        public ClinicalStatus ClinicalStatus { get; set; } = clinicalStatus;
        public ConfirmationStatus ConfirmationStatus { get; set; } = confirmationStatus;
        [GraphQLIgnore]
        public SymptomStart SymptomStart { get; set; } = symptomStart;
    }
}
