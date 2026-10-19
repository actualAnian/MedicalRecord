using Register.Builders;
using Register.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Register
{
    public interface IDiagnosisService
    {
        Result<Diagnosis> Register(RegisterDiagnosisRequest request);
    }
    public record RegisterDiagnosisRequest(
        string SourceSystem,
        string SourceRecordId,
        string PatientPesel,
        string? MedicalRecordNumber,
        string IcdCode,
        string CodingSystem,
        DateOnly DiagnosisDate,
        string ClinicalStatus,
        string ConfirmationStatus,
        DateOnly? SymptomStartDate,
        int? SymptomStartAge,
        string? VisitId
    );

    public class DiagnosisService : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repository;
        public DiagnosisService(IDiagnosisRepository repository)
        {
            _repository = repository;
        }

        public Result<Diagnosis> Register(RegisterDiagnosisRequest request)
        {
            var errors = new List<Error>();

            var existing = _repository.FindBySourceIdentity(
                request.SourceSystem,
                request.SourceRecordId);
            if (existing.IsSuccess) return Result<Diagnosis>.Success(existing.GetValueOrThrow());

            var symptomStart = SymptomStartBuilder.BuildSymptomStart(request.SymptomStartDate, request.SymptomStartAge);
            var icd10 = ICD10CodeBuilder.Build(_repository, request.CodingSystem, request.IcdCode);
            var clinicalStatus = ClinicalStatusBuilder.BuildClinicalStatus(request.ClinicalStatus);
            var confirmationStatus = ConfirmationStatusBuilder.BuildConfirmationStatus(request.ConfirmationStatus);

            if (symptomStart.IsFailure) errors.AddRange(symptomStart.Errors);
            if (icd10.IsFailure) errors.AddRange(icd10.Errors);
            if (clinicalStatus.IsFailure) errors.AddRange(clinicalStatus.Errors);
            if (confirmationStatus.IsFailure) errors.AddRange(confirmationStatus.Errors);

            if (errors.Count > 0)
                return Result<Diagnosis>.Fail(errors);

            var diagnosis = new Diagnosis
            (
                request.SourceSystem,
                request.SourceRecordId,
                new(new()),
                icd10.GetValueOrThrow(),
                request.DiagnosisDate,
                clinicalStatus.GetValueOrThrow(),
                confirmationStatus.GetValueOrThrow(),
                symptomStart.GetValueOrThrow()
            );
            _repository.Add(diagnosis);

            Func<Diagnosis> call = async () => await ClientCertificateOption.
            Retry.Execute<Diagnosis>(var response = await client.PostAsJsonAsync("/diagnoses", request);)
            return Result<Diagnosis>.Success(diagnosis);
        }
    }
}