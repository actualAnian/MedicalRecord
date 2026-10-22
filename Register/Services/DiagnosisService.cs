using Register.Builders;
using Register.DTO;
using Register.ExternalAPI;
using Register.Repositories;

namespace Register.Services
{
    public interface IDiagnosisService
    {
        Result<IEnumerable<Diagnosis>> FindDiagnosesByStatus(Guid patientId, string clinicalStatuses);
        Task<Result<Diagnosis>> Register(RegisterDiagnosisRequest request, CancellationToken cancellationToken = default);
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

    public class DiagnosisService(IDiagnosisRepository repository, IExternalRegistryClient externalRegistry) : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repository = repository;
        private readonly IExternalRegistryClient _externalRegistry = externalRegistry;

        public Result<IEnumerable<Diagnosis>> FindDiagnosesByStatus(Guid patientId, string clinicalStatus)
        {
            if (!Enum.TryParse<ClinicalStatus>(clinicalStatus, ignoreCase: true, out var result))
            {
                var enumValue = string.Join(", ", Enum.GetNames(typeof(ClinicalStatus)));
                return Result<IEnumerable<Diagnosis>>.Fail([new Error.Validation($"status not found, all statuses: {enumValue}")]);
            }
            var diagnoses = _repository.FindByStatus(patientId, result);
            return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
        }

        public async Task<Result<Diagnosis>> Register(
            RegisterDiagnosisRequest request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<Error>();

            var existing = _repository.FindBySourceIdentity(
                request.SourceSystem,
                request.SourceRecordId);
            if (existing != null) return Result<Diagnosis>.Success(existing);

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

            var external = await _externalRegistry.RegisterAsync(diagnosis, cancellationToken);
            if (external.IsFailure) return Result<Diagnosis>.Fail(external.Errors);

            return Result<Diagnosis>.Success(diagnosis);
        }
    }
}
