namespace ThirdPartyBroadcast.Consumer.Extensions;

public static class ScoreValidator
{
    public static bool ValidateScore(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo)
    {
        if (htResult != null && ftResult == null)
        {
            return htResult.TryParseScore(out var _);
        }

        if (!ftResult.TryParseScore(out var ftScore) || ftScore.Home < (matchInfo.HtHomeScore ?? 0) || ftScore.Away < (matchInfo.HtAwayScore ?? 0))
        {
            return false;
        }

        if (htResult == null)
        {
            return true;
        }

        if (!htResult.TryParseScore(out var htScore) || htScore.Home > ftScore.Home || htScore.Away > ftScore.Away)
        {
            return false;
        }

        return true;
    }
}