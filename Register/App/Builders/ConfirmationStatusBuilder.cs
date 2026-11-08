using Register.App;
using Register.App.DTO;

namespace Register.App.Builders
{
    public static class ConfirmationStatusBuilder
    {
        public static Result<ConfirmationStatus> BuildConfirmationStatus(string data)
        {
            if (!Enum.TryParse<ConfirmationStatus>(data, ignoreCase: true, out var status) ||
                !Enum.IsDefined(status))
            {
                var enumValue = string.Join(", ", Enum.GetNames<ConfirmationStatus>());
                return Result<ConfirmationStatus>.Fail(
                    [new Error.Validation($"confirmation status not found, all statuses: {enumValue}")]);
            }

            return Result<ConfirmationStatus>.Success(status);
        }
    }
}
