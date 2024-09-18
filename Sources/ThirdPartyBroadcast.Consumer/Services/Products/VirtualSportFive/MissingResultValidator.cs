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
        if (IsFtResultMissing(htResult, ftResult, matchInfo))
        {
            return new Result(4, "FT not exist");
        }

        if (IsHtResultMissing(htResult, ftResult, matchInfo))
        {
            return new Result(5, "HT not exist");
        }

        return _next?.Validate(htResult, ftResult, matchInfo) ?? new Result();
    }

    private static bool IsFtResultMissing(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        var hasHtResult = htResult != null;
        var hasFtResult = ftResult != null;
        var hasFinalScores = matchInfo.FinalAwayScore != null && matchInfo.FinalHomeScore != null;
        var hasMissingHtScores = matchInfo.HtAwayScore == null && matchInfo.HtHomeScore == null;

        return hasHtResult && !hasFtResult && hasFinalScores && hasMissingHtScores;
    }

    private static bool IsHtResultMissing(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        return htResult == null && ftResult != null
                                && matchInfo.HtAwayScore == null && matchInfo.HtHomeScore == null;
    }
}