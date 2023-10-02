namespace ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

public class LeapGamingGetMatchInfoCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match_s178_Sel";

    public override bool IsValid() => MatchId > 0;
}