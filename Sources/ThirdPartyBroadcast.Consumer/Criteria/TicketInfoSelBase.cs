using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public abstract class TicketInfoSelBase : CriteriaBase
{
    public int? MatchId { get; set; }
    public string TransIDs { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }
}