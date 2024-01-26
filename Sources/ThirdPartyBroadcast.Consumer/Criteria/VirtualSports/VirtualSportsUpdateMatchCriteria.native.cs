namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

public class VirtualSportsUpdateMatchCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Command { get; set; }

    public override string GetSettingKey() => "NAP_UpdateMatch_Remark";

    public override bool IsValid() => MatchId > 0;
}