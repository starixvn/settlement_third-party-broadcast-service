namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class GetMatchStatusCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match_s193_Sel";

    public override bool IsValid() => MatchId > 0;
}