using ThirdPartyBroadcast.Consumer.Extensions;

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

        return ftResult != null ? ValidateOnlyFtConsistency(ftResult, matchInfo) : new Result();
    }

    private static Result ValidateOnlyFtConsistency(ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        ftResult.TryParseScore(out var ftScore);
        var isHtCalculated = matchInfo.HtAwayScore != null || matchInfo.HtHomeScore != null;
        var isFtScoreHigherThanHtScore = ftScore.Home > matchInfo.HtHomeScore || ftScore.Away > matchInfo.HtAwayScore;

        if (isHtCalculated && isFtScoreHigherThanHtScore)
        {
            var errorMessage = string.Format(ErrorMessage.Inconsistent, $"{matchInfo.HtHomeScore}-{matchInfo.HtAwayScore}", $"{ftResult.Home}-{ftResult.Away}");
            return new Result(3, errorMessage);
        }

        return new Result();
    }

    private static Result ValidateOnlyHtConsistency(ResourceItemModel htResult, MatchInfo matchInfo)
    {
        htResult.TryParseScore(out var htScore);
        var isFtCalculated = IsFtScoreCalculated(matchInfo);
        var isHtScoreHigherThanFtScore = htScore.Home > matchInfo.FinalHomeScore || htScore.Away > matchInfo.FinalAwayScore;

        if (isFtCalculated && isHtScoreHigherThanFtScore)
        {
            var errorMessage = string.Format(ErrorMessage.Inconsistent, $"{htResult.Home}-{htResult.Away}", $"{matchInfo.FinalHomeScore}-{matchInfo.FinalAwayScore}");

            return new Result(3, errorMessage);
        }

        return new Result();
    }

    private static bool IsFtScoreCalculated(MatchInfo matchInfo)
    {
        return matchInfo.FinalAwayScore != null || matchInfo.FinalHomeScore != null;
    }

    private static Result ValidateHtFtConsistency(ResourceItemModel htResult, ResourceItemModel ftResult)
    {
        htResult.TryParseScore(out var htScore);
        ftResult.TryParseScore(out var ftScore);

        if (IsHtScoreHigherThanFtScore(htScore, ftScore))
        {
            var errorMessage = string.Format(ErrorMessage.Inconsistent, $"{htResult.Home}-{htResult.Away}", $"{ftResult.Home}-{ftResult.Away}");
            return new Result(3, errorMessage);
        }

        return new Result();
    }

    private static bool IsHtScoreHigherThanFtScore(Score htScore, Score ftScore)
    {
        return htScore.Home > ftScore.Home || htScore.Away > ftScore.Away;
    }

    private static Result ValidateMissingResult(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        if (IsFinalResultExistButHalfTimeResultMissing(htResult, ftResult, matchInfo))
        {
            return new Result(4, "FT not exist");
        }

        return IsHalfTimeResultMissingButFinalResultExists(htResult, ftResult, matchInfo) ? new Result(5, "HT not exist") : new Result();
    }

    private static bool IsFinalResultExistButHalfTimeResultMissing(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        return htResult != null && ftResult == null
                                && matchInfo.HtAwayScore == null && matchInfo.HtHomeScore == null
                                && matchInfo.FinalAwayScore != null && matchInfo.FinalHomeScore != null;
    }

    private static bool IsHalfTimeResultMissingButFinalResultExists(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        return htResult == null && ftResult != null
                                && matchInfo.HtAwayScore == null && matchInfo.HtHomeScore == null;
    }
}