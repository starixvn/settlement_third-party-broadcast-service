namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualBetby;

public class SettleVirtualBetbyCriteria : CalculateCriteria
{
    public int SportType { get; set; }

    public int ActionId { get; set; }

    public string Action { get; set; }

    public string SelectedResources { get; set; }

    public bool IsReSettledTickets { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_VS6_{DbServer}";
}