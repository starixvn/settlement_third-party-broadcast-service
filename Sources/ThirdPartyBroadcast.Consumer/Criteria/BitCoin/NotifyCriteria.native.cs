namespace ThirdPartyBroadcast.Consumer.Criteria.BitCoin;

public class NotifyCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public int NotifyAction { get; set; }

    public override string GetSettingKey() => "NAP_Admin_SendNotify_Bitcoin";

    public override bool IsValid() => MatchId > 0;
}