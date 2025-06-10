namespace ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;


public class GetSelectionMarketsCriteria : CriteriaBase
{
    public int MatchId { get; set; }
    public string SelectionTypes { get; set; }
    public override string GetSettingKey() => "NAP_Settlement_MarketIDSel";

    public override bool IsValid() => true;
}
