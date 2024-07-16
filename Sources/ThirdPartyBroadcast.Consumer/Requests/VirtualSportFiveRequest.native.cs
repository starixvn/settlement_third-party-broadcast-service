using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Requests;

public class VirtualSportFiveRequest
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string SelectedResources { get; set; }

    public string Ip { get; set; } = string.Empty;

    public string Action { get; set; } = ActionName.Calculate;

    public bool IsReSettledTickets { get; set; } = true;
}