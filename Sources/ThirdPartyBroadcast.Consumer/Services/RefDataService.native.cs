using Fanex.RemotingRef;

namespace ThirdPartyBroadcast.Consumer.Services
{
    public interface RefDataService
    {
        string GetLeagueName(int leagueId, string languageCode, bool is3rd = false);

        string GetTeamName(int teamId, string languageCode, bool is3rd = false);

        string GetSportName(int sportId, string languageCode);

        string GetEvent(string languageCode);
    }

    public class RefDataServiceImp(IRemotingRefService remotingRefService) : RefDataService
    {
        public string GetLeagueName(int leagueId, string languageCode, bool is3rd = false)
        {
            if (is3rd)
            {
                return remotingRefService.GetLeagueName3rd(leagueId.ToString(), languageCode, string.Empty);
            }

            return remotingRefService.GetLeagueName(leagueId.ToString(), languageCode, string.Empty);
        }

        public string GetTeamName(int teamId, string languageCode, bool is3rd = false)
        {
            if (is3rd)
            {
                return remotingRefService.GetTeamName3rd(teamId.ToString(), languageCode, string.Empty);
            }

            return remotingRefService.GetTeamName(teamId.ToString(), languageCode, string.Empty);
        }

        public string GetSportName(int sportId, string languageCode)
        {
            return remotingRefService.GetSportName(sportId.ToString(), languageCode, string.Empty);
        }

        public string GetEvent(string languageCode)
        {
            return remotingRefService.GetResourceDataByRefIdAndResourceId("BVS_MatchCode", "49", languageCode, string.Empty);
        }
    }
}