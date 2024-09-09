using ThirdPartyBroadcast.Consumer.Services;

namespace ThirdPartyBroadcast.Consumer.Models;

public class MatchInfo
{
	public RefDataService RefDataService { get; set; }

	public int MatchId { get; set; }

	public string EventStatus { get; set; }

    public short SportType { get; set; }

	public string SportName => RefDataService.GetSportName(SportType, ContentLanguage.English);

	public DateTime KickOffTime { get; set; }

	public int LeagueId { get; set; }

	public int HomeId { get; set; }

	public int AwayId { get; set; }

	public short? LeagueGroupId { get; set; }

    public string TeamName => $"{RefDataService.GetTeamName(HomeId, ContentLanguage.English)} -vs- {RefDataService.GetTeamName(AwayId, ContentLanguage.English)}";

	public string TeamNameCh => GetTeamName(ContentLanguage.Cantonese);

	public string TeamNameCs => GetTeamName(ContentLanguage.Mandarin);

    public string LeagueName => RefDataService.GetLeagueName(LeagueId, ContentLanguage.English);


	public string LeagueNameCh => GetLeagueName(ContentLanguage.Cantonese);

	public string LeagueNameCs => GetLeagueName(ContentLanguage.Mandarin);
	public string LeagueNameNewCs => GetLeagueName(ContentLanguage.NewMandarin);

	public string KickOffDate => $"{KickOffTime.Day}/{KickOffTime.Month}";

	private string GetTeamName(string languageCode)
	{
        var home = RefDataService.GetTeamName(HomeId, languageCode);
        var away = RefDataService.GetTeamName(AwayId, languageCode);

		return (string.IsNullOrEmpty(home) || string.IsNullOrEmpty(away)) ? TeamName : $"{home} -vs- {away}";
	}

	private string GetLeagueName(string languageCode)
	{
        var value = RefDataService.GetLeagueName(LeagueId, languageCode);

		return (string.IsNullOrEmpty(value)) ? LeagueName : value;
	}
}