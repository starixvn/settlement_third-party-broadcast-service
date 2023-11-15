using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.RemotingRef;

namespace ThirdPartyBroadcast.Consumer.Services
{
    public interface RefDataService
    {
        string GetLeagueName(int leagueId, string languageCode);

        string GetTeamName(int teamId, string languageCode);
    }

    public class RefDataServiceImp : RefDataService
    {
        private readonly IRemotingRefService remotingRefService;

        public RefDataServiceImp(IRemotingRefService remotingRefService)
        {
            this.remotingRefService = remotingRefService;
        }

        public string GetLeagueName(int leagueId, string languageCode)
        {
            return remotingRefService.GetLeagueName(leagueId.ToString(), languageCode, string.Empty);
        }

        public string GetTeamName(int teamId, string languageCode)
        {
            return remotingRefService.GetTeamName(teamId.ToString(), languageCode, string.Empty);
        }
    }
}