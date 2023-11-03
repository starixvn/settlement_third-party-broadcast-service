namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class VirtualBetRadarCriteria : CalculateCriteria
{
    public int Hth { get; set; }

    public int Hta { get; set; }

    public int Fth { get; set; }

    public int Fta { get; set; }

    public string ResourceResult { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_VS_BetRadar_{DbServer}";
}