using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartyBroadcast.Consumer.Criteria;
public class GetFastMarketMatchDetailCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_FastMarket_MatchSelDetail";

    public override bool IsValid() => MatchId > 0;
}
