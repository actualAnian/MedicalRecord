using Register.DTO;

namespace Register.Builders
{
    public static class ConfirmationStatusBuilder
    {
        public static Result<ConfirmationStatus> BuildConfirmationStatus(string data)
        {
            return Result<ConfirmationStatus>.Success(ConfirmationStatus.Confirmed);
        }
    }
}
