using Register.App.DTO;

namespace Register.DataBase.DTO
{
    public class DiagnosisDb
    {
        public Guid Id { get; set; }
        public string SourceSystem { get; set; } = string.Empty;
        public string SourceRecordId { get; set; } = string.Empty;
        public Guid PatientId { get; set; }
        public string IcdSystem { get; set; } = string.Empty;
        public string IcdCode { get; set; } = string.Empty;
        public DateOnly DiagnosisDate { get; set; }
        public ClinicalStatus ClinicalStatus { get; set; }
        public ConfirmationStatus ConfirmationStatus { get; set; }
        public string SymptomStartType { get; set; } = string.Empty;
        public DateOnly? SymptomStartDate { get; set; }
        public int? SymptomStartAge { get; set; }
    }
}
