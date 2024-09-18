using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

public class CorrectScoreValidator : IValidator
{
    private IValidator _next;

    public IValidator SetNext(IValidator next)
    {
        _next = next;
        return _next;
    }

    public Result Validate(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        if (htResult != null && !htResult.TryParseScore(out _))
        {
            return new Result
            {
                ErrorCode = 1,
                ErrorMessage = $"Invalid HT score: {htResult.Home}-{htResult.Away}"
            };
        }

        if (ftResult != null && !ftResult.TryParseScore(out _))
        {
            return new Result
            {
                ErrorCode = 2,
                ErrorMessage = $"Invalid FT score: {ftResult.Home}-{ftResult.Away}"
            };
        }

        return _next?.Validate(htResult, ftResult, matchInfo) ?? new Result(); // success
    }
}