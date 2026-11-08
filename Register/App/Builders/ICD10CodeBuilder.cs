using Register.App.DTO;
using Register.App.Services;
namespace Register.App.Builders
{
    class ICD10CodeBuilder
    {
        public static Result<ICD10> Build(IICD10CatalogueService catalogue, string system, string code)
        {
            var display = catalogue.GetDescriptionForCode(system, code);
            if (display is null)
                return Result<ICD10>.Fail([new Error.NotFound("ICD10 code", Guid.Empty)]);
            var icd10 = new ICD10(system, code, display);
            return Result<ICD10>.Success(icd10);
        }
    }
}