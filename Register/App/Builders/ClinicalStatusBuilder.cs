using Register.App;
using Register.App.DTO;

namespace Register.App.Builders
{
    public static class ClinicalStatusBuilder
    {
        public static Result<ClinicalStatus> BuildClinicalStatus(string data)
        {
            return Result<ClinicalStatus>.Success(ClinicalStatus.Active);
        }
    }
}
