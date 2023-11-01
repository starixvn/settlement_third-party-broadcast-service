namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchStatus
{
    public bool Calculate { get; set; }

    public bool Refund { get; set; }

    public bool CalRefund { get; set; }

    public bool Multiple { get; set; }

    public string EventStatus { get; set; }
    public DateTime KickOffTime { get; set; }

    public string LeagueName { get; set; }
    public short? LeagueGroupId { get; set; }
    public int MatchId { get; set; }
}