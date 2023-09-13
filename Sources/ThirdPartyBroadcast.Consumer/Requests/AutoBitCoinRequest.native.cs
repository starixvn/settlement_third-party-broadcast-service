namespace ThirdPartyBroadcast.Consumer.Requests;

public class AutoBitCoinRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string Action { get; set; } = "CAL";

    public string Ip { get; set; } = string.Empty;

    public bool IsAuto { get; set; } = true;

    public int ResultId { get; set; }

    public string GameResult { get; set; } = string.Empty;
}