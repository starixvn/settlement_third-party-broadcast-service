namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetby;

public class GetResultDetail3rdCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_ResultDetail3rd_Sel";

    public override bool IsValid() => MatchId > 0;
}