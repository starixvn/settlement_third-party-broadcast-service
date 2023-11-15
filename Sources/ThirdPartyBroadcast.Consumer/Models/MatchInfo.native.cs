namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchInfo
{
    public int MatchId { get; set; }

    public string EventStatus { get; set; }

    public int SportType { get; set; }

    public string SportName { get; set; }

    public DateTime KickOffTime { get; set; }

    public string LeagueName { get; set; }
    public string LeagueNameCh { get; set; }
    public string LeagueNameCs { get; set; }

    public int LeagueId { get; set; }
    public int HomeId { get; set; }
    public int AwayId { get; set; }

    public string homeName { get; set; }
    public string homeNameCh { get; set; }
    public string homeNameCs { get; set; }

    public string awayName { get; set; }
    public string awayNameCh { get; set; }
    public string awayNameCs { get; set; }

    public short? LeagueGroupId { get; set; }

    public int TotalTickets { get; set; }
}