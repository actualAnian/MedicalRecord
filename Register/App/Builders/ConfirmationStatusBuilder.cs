using Register.App;
using Register.App.DTO;

namespace Register.App.Builders
{
    public static class ConfirmationStatusBuilder
    {
        public static Result<ConfirmationStatus> BuildConfirmationStatus(string data)
        {
            return Result<ConfirmationStatus>.Success(ConfirmationStatus.Confirmed);
        }
    }
}
