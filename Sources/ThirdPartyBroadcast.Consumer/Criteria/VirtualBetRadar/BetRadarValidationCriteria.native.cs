namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetRadar;

public class BetRadarValidationCriteria : BaseCriteria
{
    public int MatchId { get; set; }

    public string ResourceResult { get; set; }

    public int? Hth { get; set; }

    public int? Hta { get; set; }

    public int? Fth { get; set; }

    public int? Fta { get; set; }

    public string Action { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_s193_Validation_{DbServer}";

    public override bool IsValid() => MatchId > 0;
}