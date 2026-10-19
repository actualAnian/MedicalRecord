using Register.DTO;

namespace Register.Builders
{
    public static class ClinicalStatusBuilder
    {
        public static Result<ClinicalStatus> BuildClinicalStatus(string data)
        {
            return Result<ClinicalStatus>.Success(ClinicalStatus.Active);
        }
    }
}
