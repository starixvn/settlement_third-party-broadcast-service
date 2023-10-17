using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class TicketSettleCriteria : CriteriaBase
{
    public int MatchId { get; set; }
    public short StatusId { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Set_TicketSettleSel_{DbServer}";

    public override bool IsValid() => true;
}