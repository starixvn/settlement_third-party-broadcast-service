namespace ThirdPartyBroadcast.Consumer.Requests;

public class HappyFiveRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public int SportType { get; set; } = 164;

    public string Action { get; set; } = "CAL";

    public int ActionId => ActionIdValue(Action);

    public string Ip { get; set; } = string.Empty;

    public bool IsAuto { get; set; } = true;

    private static int ActionIdValue(string actionName) => string.Equals(actionName, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? Constants.ActionId.HappyFiveCalc
        : Constants.ActionId.HappyFiveRefund;
}