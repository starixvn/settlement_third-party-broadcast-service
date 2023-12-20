namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

public class VirtualSportsGetMatchInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match_s180_Sel";

    public override bool IsValid() => MatchId > 0;
}