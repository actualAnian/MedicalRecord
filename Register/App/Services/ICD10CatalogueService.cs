using Register.App.DTO;

namespace Register.App.Services
{
    public class ICD10CatalogueService : IICD10CatalogueService
    {
        private readonly IEnumerable<ICD10>? ICD10Codes;
        public ICD10CatalogueService()
        {
            ICD10Codes = ICD10Reader.Read("icd10Codes.json");
        }

        public ICD10? Find(string system, string code)
        {
            return ICD10Codes?.FirstOrDefault(icd => icd.System == system && icd.Code == code);
        }

        public string? GetDescriptionForCode(string system, string code)
        {
            return ICD10Codes?.FirstOrDefault(icd => icd.System == system && icd.Code == code)?.Display;
        }
    }
}
