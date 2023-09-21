namespace ThirdPartyBroadcast.Consumer.Requests;

public class NumberGameRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; } = 148;

    public string Action { get; set; } = "CAL";

    public string Ip { get; set; }

    public bool IsAuto { get; set; } = true;

    public int DrawSeq { get; set; }

    public bool IsLive { get; set; }

    public bool IsLoop { get; set; }

    public bool IsLoopTempTable { get; set; }
}