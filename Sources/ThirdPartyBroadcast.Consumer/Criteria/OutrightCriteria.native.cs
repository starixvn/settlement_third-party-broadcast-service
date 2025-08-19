using Fanex.Data;
using System.Text.Json.Serialization;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class OutrightCriteria : CalculateCriteria
{
    public float DeadHeat { get; set; }

    public string Status { get; set; }

    public string EventDate { get; set; }

    [JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "Dot_Admin_SettleOutright_Trans";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}