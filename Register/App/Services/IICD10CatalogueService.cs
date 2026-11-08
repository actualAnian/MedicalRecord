using Register.App.DTO;

namespace Register.App.Services
{
    public interface IICD10CatalogueService
    {
        ICD10? Find(string system, string code);
        string? GetDescriptionForCode(string system, string code);
    }
}
