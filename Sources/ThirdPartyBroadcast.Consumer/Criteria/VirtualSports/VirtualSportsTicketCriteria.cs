namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

public class VirtualSportsTicketCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Admin_SendNotify_VS";

    public override bool IsValid() => MatchId > 0;
}