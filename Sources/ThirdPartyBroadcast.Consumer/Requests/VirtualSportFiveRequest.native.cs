namespace ThirdPartyBroadcast.Consumer.Requests;

public class VirtualSportFiveRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string SelectedResources { get; set; }

    public string Ip { get; set; } = string.Empty;

    public string Action { get; set; } = ActionName.Calculate;

    public int ActionId => string.Equals(Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? Constants.ActionId.VirtualSportFiveCal
        : Constants.ActionId.VirtualSportFiveRefund;

    public bool IsReSettledTickets { get; set; } = true;
}