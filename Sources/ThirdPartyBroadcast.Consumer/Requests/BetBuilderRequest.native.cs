namespace ThirdPartyBroadcast.Consumer.Requests;

public class BetBuilderRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; } = 148;

    public string Action { get; set; } = "CAL";

    public string Ip { get; set; } = string.Empty;

    public bool IsAuto { get; set; } = true;

    public int VendorId { get; set; } = 34;

    public string Resources { get; set; } = string.Empty;
}