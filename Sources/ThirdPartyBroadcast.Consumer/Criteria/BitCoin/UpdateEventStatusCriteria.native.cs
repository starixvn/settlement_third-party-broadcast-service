namespace ThirdPartyBroadcast.Consumer.Criteria.BitCoin;

public class UpdateEventStatusCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Action { get; set; }

    public override string GetSettingKey() => "NAP_Admin_Match3rd_Update";

    public override bool IsValid() => MatchId > 0;
}