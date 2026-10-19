using Register.DTO;
namespace Register.Builders
{
    class ICD10CodeBuilder
    {
        public static Result<ICD10> Build(IDiagnosisRepository repository, string system, string code)
        {
            var display = repository.GetDescriptionForCode(system, code);
            if (display is null)
                return Result<ICD10>.Fail([new Error.NotFound("ICD10 code", Guid.Empty)]);
            var icd10 = new ICD10(system, code, display);
            return Result<ICD10>.Success(icd10);
        }
    }
}