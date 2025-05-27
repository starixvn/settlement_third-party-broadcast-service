using Fanex.Data;
using System.Text.Json.Serialization;

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

    [JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_VS_BetRadar";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}