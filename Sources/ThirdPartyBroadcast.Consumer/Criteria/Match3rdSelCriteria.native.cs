namespace ThirdPartyBroadcast.Consumer.Criteria;

public class Match3rdSelCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match3rdSel";

    public override bool IsValid() => MatchId > 0;
}