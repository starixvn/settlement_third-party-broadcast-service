namespace ThirdPartyBroadcast.Consumer.Criteria;

public class WriteLogCriteria(int matchId, string errorMessage, int actionId, int userId, string ip)
    : NonQueryCommand
{
    public WriteLogCriteria(int matchId, int actionId, int userId, string ip) : this(matchId, null, actionId, userId, ip)
    {
    }

    public int UserId { get; } = userId;

    public int MatchId { get; set; } = matchId;

    public string Remark { get; set; } = errorMessage;

    public string Ip { get; set; } = ip;

    public int ActionId { get; } = actionId;

    public int? FinalHomeScore { get; set; }

    public int? FinalAwayScore { get; set; }

    public int? HtHomeScore { get; set; }

    public int? HtAwayScore { get; set; }

    public override bool IsValid() => MatchId > 0;

    public override string GetSettingKey() => "WriteLog_UserLog_Settle";
}