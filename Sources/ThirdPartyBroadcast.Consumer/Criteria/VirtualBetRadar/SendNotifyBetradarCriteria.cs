namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class SendNotifyBetRadarCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public int NotifyAction { get; set; }

    public bool IsParlay { get; set; } = true;

    public short ParlayStatus { get; set; } = 2;

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public override string GetSettingKey() => "NAP_Admin_SendNotify_VS_Betradar";

    public override bool IsValid() => true;
}