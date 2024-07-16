using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;

public class TicketInfo3rdSelCriteria : TicketInfoSelBase
{
    public short SportType { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match3rd_TicketInfo_Sel";

    public override bool IsValid() => MatchId > 0;
}