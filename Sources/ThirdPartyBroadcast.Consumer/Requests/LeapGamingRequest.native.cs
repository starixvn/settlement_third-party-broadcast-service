namespace ThirdPartyBroadcast.Consumer.Requests;

public class LeapGamingRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string SelectedResources { get; set; }

    public string Ip { get; set; } = string.Empty;

    public string Action { get; set; } = "CAL";

    public bool IsReSettledTickets { get; set; } = true;
}