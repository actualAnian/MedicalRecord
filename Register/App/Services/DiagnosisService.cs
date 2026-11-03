using Register.App.Builders;
using Register.App.DTO;
using Register.App.ExternalAPI;
using Register.DataBase.Repositories;

namespace Register.App.Services
{
    public interface IDiagnosisService
    {
        Task<Result<IEnumerable<Diagnosis>>> FindDiagnosesByStatus(Guid patientId, string clinicalStatuses);
        Task<Result<Diagnosis>> Register(RegisterDiagnosisRequest request, CancellationToken cancellationToken = default);
    }
    public record RegisterDiagnosisRequest(
        string SourceSystem,
        string SourceRecordId,
        string? PatientPesel,
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

    public class DiagnosisService(IDiagnosisRepository repository, IPatientService patientService, IICD10Catalogue iCD10Catalogue, IExternalRegistryClient externalRegistry) : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repository = repository;
        private readonly IPatientService _patientService = patientService;
        private readonly IICD10Catalogue _iCD10Catalogue = iCD10Catalogue;
        private readonly IExternalRegistryClient _externalRegistry = externalRegistry;

        public async Task<Result<IEnumerable<Diagnosis>>> FindDiagnosesByStatus(Guid patientId, string clinicalStatus)
        {
            if (!Enum.TryParse<ClinicalStatus>(clinicalStatus, ignoreCase: true, out var result))
            {
                var enumValue = string.Join(", ", Enum.GetNames(typeof(ClinicalStatus)));
                return Result<IEnumerable<Diagnosis>>.Fail([new Error.Validation($"status not found, all statuses: {enumValue}")]);
            }
            var diagnoses = await _repository.FindByStatusAsync(patientId, result);
            return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
        }

        public async Task<Result<Diagnosis>> Register(
            RegisterDiagnosisRequest request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<Error>();

            var existing = await _repository.FindBySourceIdentityAsync(
                request.SourceSystem,
                request.SourceRecordId);
            if (existing != null) return Result<Diagnosis>.Success(existing);

            var symptomStart = SymptomStartBuilder.BuildSymptomStart(request.SymptomStartDate, request.SymptomStartAge);
            var icd10 = ICD10CodeBuilder.Build(_iCD10Catalogue, request.CodingSystem, request.IcdCode);
            var clinicalStatus = ClinicalStatusBuilder.BuildClinicalStatus(request.ClinicalStatus);
            var confirmationStatus = ConfirmationStatusBuilder.BuildConfirmationStatus(request.ConfirmationStatus);

            if (symptomStart.IsFailure) errors.AddRange(symptomStart.Errors);
            if (icd10.IsFailure) errors.AddRange(icd10.Errors);
            if (clinicalStatus.IsFailure) errors.AddRange(clinicalStatus.Errors);
            if (confirmationStatus.IsFailure) errors.AddRange(confirmationStatus.Errors);

            Result<Patient> patientResult;
            if (!string.IsNullOrWhiteSpace(request.PatientPesel))
                patientResult = await _patientService.FindPatientByPeselAsync(request.PatientPesel);
            else if (!string.IsNullOrWhiteSpace(request.MedicalRecordNumber))
                patientResult = await _patientService.FindPatientBySourceIdentityAsync(request.SourceSystem, request.MedicalRecordNumber);
            else patientResult = Result<Patient>.Fail([new Error.Validation("PatientPesel or MedicalRecordNumber must be provided")]);
            if (patientResult.IsFailure)
                errors.AddRange(patientResult.Errors);
            if (errors.Count > 0)
                return Result<Diagnosis>.Fail(errors);
            var patientId = patientResult.GetValueOrThrow().Id;
            var diagnosis = new Diagnosis
            (
                Guid.NewGuid(),
                request.SourceSystem,
                request.SourceRecordId,
                patientId,
                icd10.GetValueOrThrow(),
                request.DiagnosisDate,
                clinicalStatus.GetValueOrThrow(),
                confirmationStatus.GetValueOrThrow(),
                symptomStart.GetValueOrThrow()
            );
            await _repository.AddAsync(diagnosis);

            var external = await _externalRegistry.RegisterAsync(diagnosis, cancellationToken);
            if (external.IsFailure) return Result<Diagnosis>.Fail(external.Errors);

            return Result<Diagnosis>.Success(diagnosis);
        }
    }
}
