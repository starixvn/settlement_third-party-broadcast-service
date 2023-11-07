namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSoccer;

public class VisualSoccerGetMatchInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match_s180_Sel";

    public override bool IsValid() => true;
}