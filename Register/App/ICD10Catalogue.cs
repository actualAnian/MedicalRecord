using Register.App.DTO;

namespace Register.App
{
    public interface IICD10Catalogue
    {
        ICD10? Find(string system, string code);
        string? GetDescriptionForCode(string system, string code);
    }
    public class ICD10Catalogue : IICD10Catalogue
    {
        private readonly IEnumerable<ICD10>? ICD10Codes;
        public ICD10Catalogue()
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
