namespace ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public class UpdateMarketCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public int VendorId { get; set; }

    public string Action { get; set; }

    public bool IsAuto { get; set; }

    public string Resources { get; set; }

    public int ResultStatusId { get; set; } = 2;

    public override string GetSettingKey() => "NAP_Admin_ChangedMarket_Sel";

    public override bool IsValid() => MatchId > 0;
}