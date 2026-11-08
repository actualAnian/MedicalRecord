using Register.App;
using Register.App.DTO;

namespace Register.App.Builders
{
    public static class ClinicalStatusBuilder
    {
        public static Result<ClinicalStatus> BuildClinicalStatus(string data)
        {
            if (!Enum.TryParse<ClinicalStatus>(data, ignoreCase: true, out var status) ||
                !Enum.IsDefined(status))
            {
                var enumValue = string.Join(", ", Enum.GetNames<ClinicalStatus>());
                return Result<ClinicalStatus>.Fail(
                    [new Error.Validation($"status not found, all statuses: {enumValue}")]);
            }

            return Result<ClinicalStatus>.Success(status);
        }
    }
}
