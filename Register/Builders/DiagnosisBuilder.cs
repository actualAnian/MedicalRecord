using Register.DTO;

namespace Register.Builders
{
    public class DiagnosisBuilder
    {
        public static List<ISpecification<Diagnosis>> SetTypicalDiagnosisSpecifications()
        {
            return
            [
            //    DiagnosisSpecification.IsAdult().Or(
            //    DiagnosisSpecification.IsMinor().And(DiagnosisSpecification.HasValidName())
            //),
            DiagnosisSpecification.HasSourceSystem(),
            DiagnosisSpecification.HasSourceRecordId(),
            DiagnosisSpecification.HasSourceRecordId(),
            DiagnosisSpecification.HasPatient(),
            DiagnosisSpecification.HasIcdCode(),
            DiagnosisSpecification.HasSymptomStart(),
            DiagnosisSpecification.DiagnosisDateNotInTheFuture(),
            ];
        }

        private string? _sourceSystem;
        private string? _sourceRecordId;
        private Patient? _patient;
        private ICD10? _icdCode;
        private DateOnly? _diagnosisDate;
        private ClinicalStatus? _clinicalStatus;
        private ConfirmationStatus? _confirmationStatus;
        private SymptomStart? _symptomStart;

        public DiagnosisBuilder WithSourceSystem(string sourceSystem)
        {
            _sourceSystem = sourceSystem;
            return this;
        }

        public DiagnosisBuilder WithSourceRecordId(string sourceRecordId)
        {
            _sourceRecordId = sourceRecordId;
            return this;
        }

        public DiagnosisBuilder WithPatient(Patient patient)
        {
            _patient = patient;
            return this;
        }

        public DiagnosisBuilder WithIcdCode(ICD10 icdCode)
        {
            _icdCode = icdCode;
            return this;
        }

        public DiagnosisBuilder WithDiagnosisDate(DateOnly diagnosisDate)
        {
            _diagnosisDate = diagnosisDate;
            return this;
        }

        public DiagnosisBuilder WithClinicalStatus(ClinicalStatus clinicalStatus)
        {
            _clinicalStatus = clinicalStatus;
            return this;
        }

        public DiagnosisBuilder WithConfirmationStatus(
            ConfirmationStatus confirmationStatus)
        {
            _confirmationStatus = confirmationStatus;
            return this;
        }

        public DiagnosisBuilder WithSymptomStart(SymptomStart symptomStart)
        {
            _symptomStart = symptomStart;
            return this;
        }

        public Diagnosis Build()
        {

            var newDiagnosis = new Diagnosis(
                _sourceSystem,
                _sourceRecordId,
                _patient,
                _icdCode,
                _diagnosisDate.Value,
                _clinicalStatus.Value,
                _confirmationStatus.Value,
                _symptomStart);

            var spec = SetTypicalDiagnosisSpecifications();
            foreach (var specification in from specification in spec
                                          where !specification.IsSatisfiedBy(newDiagnosis)
                                          select specification)
                Console.WriteLine($"Patient does not satisfy specification: {specification.GetType().Name}");

            return newDiagnosis;

        }
    }
}
