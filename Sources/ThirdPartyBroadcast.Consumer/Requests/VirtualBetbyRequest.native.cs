namespace ThirdPartyBroadcast.Consumer.Requests;

public class VirtualBetbyRequest
{
    public int MatchId { get; set; }

    public short SportType { get; set; } = Constants.SportType.VirtualBetbyBasketball;

    public int AdminId { get; set; }

    public string SelectedResources { get; set; }

    public string Ip { get; set; } = string.Empty;

    public string Action { get; set; } = ActionName.Calculate;

    public int ActionId => string.Equals(Action, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? Constants.ActionId.VirtualBetbyCal
        : Constants.ActionId.VirtualBetbyRefund;

    public bool IsReSettledTickets { get; set; }

    public List<ResourceItemModel> Resources { get; set; } = [];
}