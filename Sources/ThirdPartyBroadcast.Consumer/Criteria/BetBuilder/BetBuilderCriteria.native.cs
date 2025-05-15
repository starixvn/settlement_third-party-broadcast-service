using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public class BetBuilderCriteria : CalculateCriteria
{
    public int VendorId { get; set; }

    public string SelectedSelection { get; set; }

    public string SelectedMarket { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public string BroadcastSessionId { get; } = Guid.NewGuid().ToString();

    [System.Text.Json.Serialization.JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_BetBuilder_Push";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}