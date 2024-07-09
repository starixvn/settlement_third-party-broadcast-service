namespace ThirdPartyBroadcast.Consumer.Models;

public class Match3rd
{
    public int MatchId { get; set; }
    public short SportType { get; set; }
    public int LeagueId { get; set; }
    public int HomeId { get; set; }
    public int AwayId { get; set; }
    public string EventStatus { get; set; }
    public DateTime EventDate { get; set; }
}