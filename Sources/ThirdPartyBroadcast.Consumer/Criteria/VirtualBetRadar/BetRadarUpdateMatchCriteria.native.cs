namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class BetRadarUpdateMatchCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Action { get; set; }

    public int AdminId { get; set; }

    public bool IsAuto { get; set; }

    public int Hth { get; set; }

    public int Hta { get; set; }

    public int Fth { get; set; }

    public int Fta { get; set; }

    public string ResourceResult { get; set; }

    public override string GetSettingKey() => "NAP_Admin_Match_s193_Upd";

    public override bool IsValid() => MatchId > 0;
}