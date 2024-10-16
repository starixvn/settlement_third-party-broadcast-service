namespace ThirdPartyBroadcast.Consumer.Requests;

public class VirtualRacingRequest
{
    public int EventId { get; set; }

    public byte Racers { get; set; }

    public int AdminId { get; set; }

    public short SportType { get; set; }

    public string Action { get; set; } = ActionName.Calculate;

    public int ActionId => ActionIdValue(Action);

    public string Ip { get; set; } = string.Empty;

    private static int ActionIdValue(string actionName) => string.Equals(actionName, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? Constants.ActionId.VirtualBetRadarCalc
        : Constants.ActionId.VirtualBetRadarRefund;
}