namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

public class MissingResultValidator : IValidator
{
    private IValidator _next;

    public IValidator SetNext(IValidator next)
    {
        _next = next;
        return _next;
    }

    public Result Validate(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        if (htResult != null && ftResult == null
            && matchInfo.HtAwayScore == null && matchInfo.HtHomeScore == null
            && matchInfo.FinalAwayScore != null && matchInfo.FinalHomeScore != null) // ht is running but ft is not exist
        {
            return new Result
            {
                ErrorCode = 4,
                ErrorMessage = "FT not exist"
            };
        }

        if (htResult == null && ftResult != null
            && matchInfo.HtAwayScore == null && matchInfo.HtHomeScore == null)
        {
            return new Result
            {
                ErrorCode = 5,
                ErrorMessage = "HT not exist"
            };
        }

        return _next?.Validate(htResult, ftResult, matchInfo) ?? new Result();
    }
}