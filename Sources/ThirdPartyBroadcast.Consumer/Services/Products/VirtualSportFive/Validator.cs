using ThirdPartyBroadcast.Consumer.Extensions;
using ThirdPartyBroadcast.Consumer.Models;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

public static class Validator
{
    public static Result ValidateScores(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        var result = ValidateCorrectScore(htResult, ftResult);
        if (!result.IsSuccessful)
        {
            return result;
        }

        result = ValidateMatchScore(htResult, ftResult, matchInfo);
        if (!result.IsSuccessful)
        {
            return result;
        }

        result = ValidateMissingResult(htResult, ftResult, matchInfo);
        return result;
    }

    private static Result ValidateCorrectScore(ResourceItemModel htResult, ResourceItemModel ftResult)
    {
        if (htResult != null && !htResult.TryParseScore(out _))
        {
            return new Result
            {
                ErrorCode = 1,
                ErrorMessage = $"Invalid HT score: {htResult.Home}-{htResult.Away}",
            };
        }

        if (ftResult != null && !ftResult.TryParseScore(out _))
        {
            return new Result
            {
                ErrorCode = 2,
                ErrorMessage = $"Invalid FT score: {ftResult.Home}-{ftResult.Away}",
            };
        }

        return new Result();
    }

    private static Result ValidateMatchScore(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        if (htResult != null && ftResult != null)
        {
            return ValidateHtFtConsistency(htResult, ftResult);
        }

        if (htResult != null)
        {
            return ValidateOnlyHtConsistency(htResult, matchInfo);
        }

        if (ftResult != null)
        {
            return ValidateOnlyFtConsistency(ftResult, matchInfo);
        }

        return new Result();
    }

    private static Result ValidateOnlyFtConsistency(ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        ftResult.TryParseScore(out var ftScore);
        var isHtCalculated = matchInfo.HtAwayScore != null || matchInfo.HtHomeScore != null;

        if (isHtCalculated && (matchInfo.HtHomeScore > ftScore.Home || matchInfo.HtAwayScore > ftScore.Away))
        {
            var message = string.Format(ErrorMessage.Inconsistent, $"{matchInfo.HtHomeScore}-{matchInfo.HtAwayScore}", $"{ftResult.Home}-{ftResult.Away}");
            return new Result(3, message);
        }
        return new Result(); // Success
    }

    private static Result ValidateOnlyHtConsistency(ResourceItemModel htResult, MatchInfo matchInfo)
    {
        htResult.TryParseScore(out var htScore);
        var ísFtCalculated = matchInfo.FinalAwayScore != null || matchInfo.FinalHomeScore != null;

        if (ísFtCalculated && (htScore.Home > matchInfo.FinalHomeScore || htScore.Away > matchInfo.FinalAwayScore))
        {
            var message = string.Format(ErrorMessage.Inconsistent, $"{htResult.Home}-{htResult.Away}", $"{matchInfo.FinalHomeScore}-{matchInfo.FinalAwayScore}");
            return new Result(3, message);
        }
        return new Result(); // Success
    }

    private static Result ValidateHtFtConsistency(ResourceItemModel htResult, ResourceItemModel ftResult)
    {
        htResult.TryParseScore(out var htScore);
        ftResult.TryParseScore(out var ftScore);

        if (htScore.Home > ftScore.Home || htScore.Away > ftScore.Away)
        {
            var message = string.Format(ErrorMessage.Inconsistent, $"{htResult.Home}-{htResult.Away}", $"{ftResult.Home}-{ftResult.Away}");
            return new Result(3, message);
        }
        return new Result(); // Success
    }

    private static Result ValidateMissingResult(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
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

        return new Result();
    }
}