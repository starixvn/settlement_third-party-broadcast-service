using Fanex.Data;
using System.Text.Json.Serialization;

namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSports;

public class VirtualSportsCriteria : CalculateCriteria
{
    public int Fth { get; set; }

    public int Fta { get; set; }

    public string Action { get; set; }

    [JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_VS_180";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}