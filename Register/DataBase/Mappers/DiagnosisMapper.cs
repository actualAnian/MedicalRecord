using Register.App.DTO;
using Register.App.Services;
using Register.DataBase.DTO;
using System;

namespace Register.DataBase.Mappers
{
    public static class DiagnosisMapper
    {
        public static DiagnosisDb ToDatabase(Diagnosis diagnosis)
        {
            var result = new DiagnosisDb
            {
                Id = diagnosis.Id,
                SourceSystem = diagnosis.SourceSystem,
                SourceRecordId = diagnosis.SourceRecordId,
                PatientId = diagnosis.PatientId,
                IcdSystem = diagnosis.IcdCode.System,
                IcdCode = diagnosis.IcdCode.Code,
                DiagnosisDate = diagnosis.DiagnosisDate,
                ClinicalStatus = diagnosis.ClinicalStatus,
                ConfirmationStatus = diagnosis.ConfirmationStatus
            };

            switch (diagnosis.SymptomStart)
            {
                case SymptomStart.FromDate fromDate:
                    result.SymptomStartType = "FromDate";
                    result.SymptomStartDate = fromDate.Date;
                    break;

                case SymptomStart.PatientAge patientAge:
                    result.SymptomStartType = "PatientAge";
                    result.SymptomStartAge = patientAge.Age;
                    break;

                default:
                    throw new ArgumentException(
                        "Unsupported SymptomStart type.");
            }

            return result;
        }

        internal static Diagnosis ToDomain(DiagnosisDb dbDiagnosis, IICD10CatalogueService catalogue)
        {
            var icd10 = catalogue.Find(dbDiagnosis.IcdSystem, dbDiagnosis.IcdCode)
                ?? throw new InvalidOperationException(
                    $"ICD10 code '{dbDiagnosis.IcdSystem}/{dbDiagnosis.IcdCode}' was not found in the catalogue.");

            return new Diagnosis(dbDiagnosis.Id,
                dbDiagnosis.SourceSystem,
                dbDiagnosis.SourceRecordId,
                dbDiagnosis.PatientId,
                icd10,
                dbDiagnosis.DiagnosisDate,
                dbDiagnosis.ClinicalStatus,
                dbDiagnosis.ConfirmationStatus,
                dbDiagnosis.SymptomStartType switch
                {
                    "FromDate" => new SymptomStart.FromDate(dbDiagnosis.SymptomStartDate!.Value),
                    "PatientAge" => new SymptomStart.PatientAge(dbDiagnosis.SymptomStartAge!.Value),
                    _ => throw new ArgumentException("Unsupported SymptomStart type.")
                });
        }
    }
}
