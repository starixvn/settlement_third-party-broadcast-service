using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetby;

public class SettleVirtualBetbyCriteria : CalculateCriteria
{
    public int SportType { get; set; }

    public int ActionId { get; set; }

    public string Action { get; set; }

    public string SelectedResources { get; set; }

    public bool IsReSettledTickets { get; set; }

    [System.Text.Json.Serialization.JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_VS6";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}