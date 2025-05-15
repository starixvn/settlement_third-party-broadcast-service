using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class FastMarketCriteria : CalculateCriteria
{
    public string Results { get; set; }

    public bool IsSettleDetermined { get; set; }

    public bool IsReSettledTickets { get; set; }

    public string EventCounters { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public bool IsOrg { get; set; } = true;

    public string BroadcastSessionId { get; } = Guid.NewGuid().ToString();

    [System.Text.Json.Serialization.JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_Fast_Market_Auto_1s_Push";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}