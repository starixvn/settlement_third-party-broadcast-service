namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;

public class VirtualSportFiveSettleCriteria : CalculateCriteria
{
    public int SportType { get; set; }
    public DateTime EventDate { get; set; }
    public int ActionId { get; set; }
    public string Action { get; set; }
    public string SelectedResources { get; set; }
    public bool IsReSettledTickets { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_VS5_{DbServer}";
}