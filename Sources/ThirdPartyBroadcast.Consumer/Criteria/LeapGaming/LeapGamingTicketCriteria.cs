namespace ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

public class LeapGamingTicketCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public override string GetSettingKey() => "NAP_Admin_SendNotify_s178";

    public override bool IsValid() => MatchId > 0;
}