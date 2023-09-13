namespace ThirdPartyBroadcast.Consumer.Criteria.BitCoin;

public class BitCoinEventStatusCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match3rdSel";

    public override bool IsValid() => MatchId > 0;
}