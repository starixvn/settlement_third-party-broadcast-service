using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSoccer;

public class VisualSoccerGetMatchInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match_s180_Sel";

    public override bool IsValid() => true;
}