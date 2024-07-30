using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        var isRequestScoreValid = ftResult != null && (htScore.Home > ftScore.Home || htScore.Away > ftScore.Away);

        var isMatchScoreValid = matchInfo.FinalHomeScore != null
            && matchInfo.FinalAwayScore != null
            && (htScore.Home > matchInfo.FinalHomeScore || htScore.Away > matchInfo.FinalAwayScore);

        if (isRequestScoreValid || isMatchScoreValid)
        {
            return new Result
            {
                ErrorCode = 3,
                ErrorMessage = $"HT({htResult?.Home}-{htResult?.Away}) is inconsistent with FT({ftResult?.Home}-{ftResult?.Away})",
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