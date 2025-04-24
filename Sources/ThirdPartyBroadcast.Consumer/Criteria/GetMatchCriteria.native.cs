using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class GetMatchCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    [SpParam(Ignored = true)]
    public bool IsPinGoal { get; set; }

    public override string GetSettingKey() => IsPinGoal ? "NAP_Set_MatchSelInfo" : "NAP_Set_Match_VS_Sel";

    public override bool IsValid() => MatchId > 0;
}