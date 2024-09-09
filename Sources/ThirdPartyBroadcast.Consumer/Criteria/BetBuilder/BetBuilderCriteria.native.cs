namespace ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public class BetBuilderCriteria : CalculateCriteria
{
    public int VendorId { get; set; }

    public string SelectedSelection { get; set; }

    public string SelectedMarket { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public string BroadcastSessionId { get; } = Guid.NewGuid().ToString();

    public override string GetSettingKey() => $"NAP_Admin_Settlement_BetBuilder_Push_{DbServer}";
}