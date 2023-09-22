namespace ThirdPartyBroadcast.Consumer.Requests;

public class FastMarketRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string Action { get; set; }

    public string Ip { get; set; } = string.Empty;

    public bool IsAuto { get; set; }

    public bool IsReSettledTickets { get; set; } = true;

    public bool IsSettleDetermined { get; set; }

    public string EventCounters { get; set; }

    public string Results { get; set; }
}