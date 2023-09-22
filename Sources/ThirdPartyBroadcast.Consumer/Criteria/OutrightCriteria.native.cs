namespace ThirdPartyBroadcast.Consumer.Criteria;

public class OutrightCriteria : CalculateCriteria
{
    public float DeadHeat { get; set; }

    public string Status { get; set; }

    public string EventDate { get; set; }

    public override string GetSettingKey() => $"Dot_Admin_SettleOutright_Trans_{DbServer}";
}