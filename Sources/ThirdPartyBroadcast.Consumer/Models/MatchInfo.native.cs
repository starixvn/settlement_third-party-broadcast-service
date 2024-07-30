using ThirdPartyBroadcast.Consumer.Services;

namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchInfo
{
	public RefDataService RefDataService { get; set; }

	public int MatchId { get; set; }
    public bool IsMatch3rd { get; set; } = false;
	public string EventStatus { get; set; }
    public DateTime EventDate { get; set; }

    public short SportType { get; set; }

	public string SportName => RefDataService.GetSportName(SportType, ContentLanguage.English);

	public DateTime KickOffTime { get; set; }

	public int LeagueId { get; set; }

	public int HomeId { get; set; }

	public int AwayId { get; set; }

	public short? LeagueGroupId { get; set; }

    public string TeamName => $"{RefDataService.GetTeamName(HomeId, ContentLanguage.English, IsMatch3rd)} -vs- {RefDataService.GetTeamName(AwayId, ContentLanguage.English, IsMatch3rd)}";
		=> $"{RefDataService.GetTeamName(HomeId, ContentLanguage.English)} -vs- {RefDataService.GetTeamName(AwayId, ContentLanguage.English)}";

	public string TeamNameCh => GetTeamName(ContentLanguage.Cantonese);

	public string TeamNameCs => GetTeamName(ContentLanguage.Mandarin);

	public string TeamNameNewCs => GetTeamName(ContentLanguage.NewMandarin);


	public string LeagueNameCh => GetLeagueName(ContentLanguage.Cantonese);

	public string LeagueNameCs => GetLeagueName(ContentLanguage.Mandarin);
	public string LeagueNameNewCs => GetLeagueName(ContentLanguage.NewMandarin);

	public string KickOffDate => $"{KickOffTime.Day}/{KickOffTime.Month}";

	public int? HtHomeScore { get; set; }
	public int? HtAwayScore { get; set; }
	public int? FinalHomeScore { get; set; }
	public int? FinalAwayScore { get; set; }

	private string GetTeamName(string languageCode)
	{
        var home = RefDataService.GetTeamName(HomeId, languageCode, IsMatch3rd);
        var away = RefDataService.GetTeamName(AwayId, languageCode, IsMatch3rd);

		return (string.IsNullOrEmpty(home) || string.IsNullOrEmpty(away)) ? TeamName : $"{home} -vs- {away}";
	}

	private string GetLeagueName(string languageCode)
	{
        var value = RefDataService.GetLeagueName(LeagueId, languageCode, IsMatch3rd);

		return (string.IsNullOrEmpty(value)) ? LeagueName : value;
	}
}