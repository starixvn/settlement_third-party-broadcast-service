namespace ThirdPartyBroadcast.Consumer.Requests;

public class PinGoalRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string Action { get; set; } = "CAL";

    public string Ip { get; set; } = string.Empty;

    public bool IsAuto { get; set; }

    public string BetStatus { get; set; }

    public bool IsReSettledTickets { get; set; } = true;

    public string SelectedResources { get; set; }
}