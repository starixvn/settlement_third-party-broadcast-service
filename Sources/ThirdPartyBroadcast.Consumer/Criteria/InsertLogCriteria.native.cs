namespace ThirdPartyBroadcast.Consumer.Criteria;
public class InsertLogCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Action { get; set; }

    public int SportType { get; } = 1;

    public bool IsAuto { get; set; }

    public int AdminId { get; set; }

    public string SelectedResources { get; set; }

    public override string GetSettingKey() => "NAP_SettlementLog_Ins";

    public override bool IsValid() => MatchId > 0;
}
