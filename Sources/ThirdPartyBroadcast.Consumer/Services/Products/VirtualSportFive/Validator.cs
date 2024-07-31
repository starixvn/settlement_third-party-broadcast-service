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
        htResult.TryParseScore(out var htScore);
        ftResult.TryParseScore(out var ftScore);

        var isHtCalculated = matchInfo.HtAwayScore != null || matchInfo.HtHomeScore != null;
        var ísFtCalculated = matchInfo.FinalAwayScore != null || matchInfo.FinalHomeScore != null;

        var isFtRequestScoreValid = ftResult != null && (htScore.Home > ftScore.Home || htScore.Away > ftScore.Away);

        var isFtMatchScoreValid = ftResult != null
            && isHtCalculated
            && (matchInfo.HtHomeScore > ftScore.Home || matchInfo.HtAwayScore > ftScore.Away);

        var isMatchScoreValid = htResult != null
            && ísFtCalculated
            && (htScore.Home > matchInfo.FinalHomeScore || htScore.Away > matchInfo.FinalAwayScore);

        if (isFtRequestScoreValid || isFtMatchScoreValid || isMatchScoreValid)
        {
            var haftScore = htResult != null ? $"HT Request({htResult.Home}-{htResult.Away})" : "";
            var finalScore = ftResult != null ? $"FT Request({ftResult.Home}-{ftResult.Away})" : "";
            return new Result
            {
                ErrorCode = 3,
                ErrorMessage = $"{haftScore} HT({matchInfo.HtHomeScore}-{matchInfo.HtAwayScore}) is inconsistent with {finalScore} FT({matchInfo.FinalHomeScore}-{matchInfo.FinalAwayScore})",
            };
        }

        return new Result();
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