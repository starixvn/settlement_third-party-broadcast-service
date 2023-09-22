namespace ThirdPartyBroadcast.Consumer.Criteria;

public class FastMarketCriteria : CalculateCriteria
{
    public string Results { get; set; }

    public bool IsSettleDetermined { get; set; }

    public bool IsReSettledTickets { get; set; }

    public string EventCounters { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_Fast_Market_Auto_1s_Push_{DbServer}";
}