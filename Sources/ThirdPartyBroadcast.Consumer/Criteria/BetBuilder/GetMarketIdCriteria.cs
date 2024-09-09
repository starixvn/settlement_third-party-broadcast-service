namespace ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public class GetMarketIdCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Action { get; set; }

    public string SelectedSelection { get; set; }

    public int ResultStatusId => 2;

    public override string GetSettingKey() => "NAP_Admin_ChangedMarket_Sel";

    public override bool IsValid() => MatchId > 0;
}