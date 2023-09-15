namespace ThirdPartyBroadcast.Consumer.Criteria.OutRight;

public class OutRightCriteria : CalculateCriteria
{
    public float DeadHeat { get; set; }

    public string Status { get; set; }

    public string EventDate { get; set; }

    public override string GetSettingKey() => $"Dot_Admin_SettleOutright_Trans_{DbServer}";
}