namespace ThirdPartyBroadcast.Consumer.Requests;

public class OutrightRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; } = 5_838_421;

    public string Action { get; set; }

    public string Status { get; set; }

    public float DeadHeat { get; set; } = 1;

    public string EventDate { get; set; }
}