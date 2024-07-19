namespace ThirdPartyBroadcast.Consumer.Models;

public class Result
{
    public int ErrorCode { get; set; }

    public string ErrorMessage { get; set; }

    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage) && ErrorCode == 0;

    public static Result ErrorRequest() => new Result
    {
        ErrorCode = Constants.ErrorCode.NotExistValue,
        ErrorMessage = Constants.ErrorMessage.NotExistValue
    };
}