using ThirdPartyBroadcast.Consumer.Services;

namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchInfo
{
    public RefDataService RefDataService { get; set; }
    public int MatchId { get; set; }

    public string EventStatus { get; set; }

    public int SportType { get; set; }

    public string SportName { get; set; }

    public DateTime KickOffTime { get; set; }

    public int LeagueId { get; set; }

    public int HomeId { get; set; }

    public int AwayId { get; set; }

    public short? LeagueGroupId { get; set; }

    public int TotalTickets { get; set; }

    public string TeamName => GetTeamName(ContentLanguage.English);

    public string TeamNameCh => GetTeamName(ContentLanguage.Cantonese, TeamName);

    public string TeamNameCs => GetTeamName(ContentLanguage.Mandarin, TeamName);

    public string LeagueName => GetLeagueName(ContentLanguage.English);

    public string LeagueNameCh => GetLeagueName(ContentLanguage.Cantonese, LeagueName);

    public string LeagueNameCs => GetLeagueName(ContentLanguage.Mandarin, LeagueName);

    public string KickOffDate => $"{KickOffTime.Day}/{KickOffTime.Month}";

    private string GetTeamName(string languageCode, string teamName = "")
    {
        if (RefDataService is null)
        {
            return string.Empty;
        }

        var home = RefDataService.GetTeamName(HomeId, languageCode);
        var away = RefDataService.GetTeamName(AwayId, languageCode);

        return (string.IsNullOrEmpty(home) || string.IsNullOrEmpty(away)) ? teamName : $"{home} -vs- {away}";
    }

    private string GetLeagueName(string languageCode, string leagueName = "")
    {
        if (RefDataService is null)
        {
            return string.Empty;
        }

        var value = RefDataService.GetLeagueName(LeagueId, languageCode);

        return (string.IsNullOrEmpty(value)) ? leagueName : value;
    }
}