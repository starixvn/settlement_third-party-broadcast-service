namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchInfo
{
    public int MatchId { get; set; }

    public string EventStatus { get; set; }

    public int SportType { get; set; }

    public string SportName { get; set; }

    public string KickOffTime { get; set; }

    public int TotalTickets { get; set; }
}