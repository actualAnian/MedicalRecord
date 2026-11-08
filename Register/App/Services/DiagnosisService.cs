using Register.App.Builders;
using Register.App.DTO;
using Register.App.ExternalAPI;
using Register.DataBase.Repositories;

namespace Register.App.Services
{
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

    public record ChangeDiagnosisStatusRequest(
        string SourceSystem,
        string SourceDiagnosisRecordId,
        string ClinicalStatus
    );

    public class DiagnosisService(IDiagnosisRepository repository, IPatientService patientService, IICD10CatalogueService iCD10Catalogue, IExternalRegistryClient externalRegistry) : IDiagnosisService
    {
        private readonly IDiagnosisRepository _repository = repository;
        private readonly IPatientService _patientService = patientService;
        private readonly IICD10CatalogueService _iCD10Catalogue = iCD10Catalogue;
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

        public async Task<Result<Diagnosis>> ChangeStatus(
            ChangeDiagnosisStatusRequest request,
            CancellationToken cancellationToken = default)
        {

            if (!Enum.TryParse<ClinicalStatus>(request.ClinicalStatus, ignoreCase: true, out var targetStatus))
            {
                var enumValue = string.Join(", ", Enum.GetNames<ClinicalStatus>());
                return Result<Diagnosis>.Fail([new Error.Validation($"status not found, all statuses: {enumValue}")]);
            }

            var diagnosis = await _repository.FindBySourceIdentityAsync(request.SourceSystem, request.SourceDiagnosisRecordId);
            if (diagnosis is null)
                return Result<Diagnosis>.Fail([new Error.NotFoundOnLookup("Diagnosis", $"Diagnosis with ID {request.SourceDiagnosisRecordId} not found")]);

            DiagnosisState? state = diagnosis.ClinicalStatus switch
            {
                ClinicalStatus.Active => new DiagnosisState.Active(),
                ClinicalStatus.Healed => new DiagnosisState.Healed(),
                _ => null
            };
            if (state is null)
                return Result<Diagnosis>.Fail([new Error.Validation("Invalid command for current diagnosis state.")]);

            DiagnosisCommand? command = targetStatus switch
            {
                ClinicalStatus.Healed => new DiagnosisCommand.SetAsHealed(diagnosis.PatientId),
                _ => null
            };
            if (command is null)
                return Result<Diagnosis>.Fail([new Error.Validation($"Unsupported status transition to {targetStatus}.")]);

            var outcome = ChangeStatusDecider.Decide(command, state, DateTime.UtcNow);
            if (outcome.Refusal is not null)
                return Result<Diagnosis>.Fail([new Error.Validation(outcome.Refusal.Message)]);

            var newState = ChangeStatusDecider.Evolve(outcome.Fact!, state);
            diagnosis.ClinicalStatus = newState switch
            {
                DiagnosisState.Healed => ClinicalStatus.Healed,
                DiagnosisState.Active => ClinicalStatus.Active,
                _ => throw new InvalidOperationException("Unsupported diagnosis state.")
            };

            await _repository.UpdateAsync(diagnosis);
            return Result<Diagnosis>.Success(diagnosis);
        }

        public async Task<Result<int>> GetAmountOfDiagnosisWithICDCode(string icdSystem, string icdCode)
        {
            if (string.IsNullOrWhiteSpace(icdSystem) ||
                string.IsNullOrWhiteSpace(icdCode))
            {
                return Result<int>.Fail(
                    [new Error.Validation("IcdSystem and IcdCode must be provided")]);
            }

            var result = await _repository.GetAmountOfDiagnosisWithICDCode(icdSystem, icdCode);
            return Result<int>.Success(result);
        }
    }
}
