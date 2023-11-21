namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchStatus
{
    public bool Calculate { get; set; }

    public bool Refund { get; set; }

    public bool CalRefund { get; set; }

    public bool Multiple { get; set; }

    public string EventStatus { get; set; }

    public DateTime KickOffTime { get; set; }

    public short? LeagueGroupId { get; set; }

    public int LeagueId { get; set; }

    public int HomeId { get; set; }

    public int AwayId { get; set; }

    public int MatchId { get; set; }
}