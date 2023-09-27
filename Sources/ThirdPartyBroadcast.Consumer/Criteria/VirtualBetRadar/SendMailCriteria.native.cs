namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class SendMailCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string SourceName { get; set; } = "NAP-Settlement-VS-BetRadar-Single";

    public int AdminId { get; set; }

    public string ActionId { get; set; }

    public string Message { get; set; }

    public string Severity { get; set; } = "Error";

    public string ProcName { get; set; } = "NAP_Admin_Settlement_VS_BetRadar_Push";

    public string Remark { get; set; }

    public string GroupName { get; set; } = "ERROR";

    public override string GetSettingKey() => "NAP_Admin_SendMail";

    public override bool IsValid() => MatchId > 0;
}