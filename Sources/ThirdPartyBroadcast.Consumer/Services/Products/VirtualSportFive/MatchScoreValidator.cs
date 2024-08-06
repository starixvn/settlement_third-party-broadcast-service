namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

public class MatchScoreValidator : IValidator
{
    private IValidator _next;

    public IValidator SetNext(IValidator next)
    {
        _next = next;
        return _next;
    }

    public Result Validate(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        var result = new Result();
        if (htResult != null && ftResult != null)
        {
            result = ValidateHtFtConsistency(htResult, ftResult);
        }

        if (htResult != null)
        {
            result = ValidateOnlyHtConsistency(htResult, matchInfo);
        }

        if (ftResult != null)
        {
            result = ValidateOnlyFtConsistency(ftResult, matchInfo);
        }

        return result.IsSuccessful ? _next?.Validate(htResult, ftResult, matchInfo) : result;
    }

    private static Result ValidateOnlyFtConsistency(ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        var isHtCalculated = matchInfo.HtAwayScore != null || matchInfo.HtHomeScore != null;
        var inValid = matchInfo.HtHomeScore > int.Parse(ftResult.Home) || matchInfo.HtAwayScore > int.Parse(ftResult.Away);

        if (isHtCalculated && inValid)
        {
            var message = string.Format(ErrorMessage.Inconsistent, $"{matchInfo.HtHomeScore}-{matchInfo.HtAwayScore}", $"{ftResult.Home}-{ftResult.Away}");
            return new Result(3, message);
        }
        return new Result();
    }

    private static Result ValidateOnlyHtConsistency(ResourceItemModel htResult, MatchInfo matchInfo)
    {
        var ísFtCalculated = matchInfo.FinalAwayScore != null || matchInfo.FinalHomeScore != null;
        var inValid = int.Parse(htResult.Home) > matchInfo.FinalHomeScore || int.Parse(htResult.Away) > matchInfo.FinalAwayScore;

        if (ísFtCalculated && inValid)
        {
            var message = string.Format(ErrorMessage.Inconsistent, $"{htResult.Home}-{htResult.Away}", $"{matchInfo.FinalHomeScore}-{matchInfo.FinalAwayScore}");
            return new Result(3, message);
        }

        return new Result();
    }

    private static Result ValidateHtFtConsistency(ResourceItemModel htResult, ResourceItemModel ftResult)
    {
        if (int.Parse(htResult.Home) > int.Parse(ftResult.Home)
            || int.Parse(htResult.Away) > int.Parse(ftResult.Away))
        {
            var message = string.Format(ErrorMessage.Inconsistent, $"{htResult.Home}-{htResult.Away}", $"{ftResult.Home}-{ftResult.Away}");
            return new Result(3, message);
        }
        return new Result();
    }
}