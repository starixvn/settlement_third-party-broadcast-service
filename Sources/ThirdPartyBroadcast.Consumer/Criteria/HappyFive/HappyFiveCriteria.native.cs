namespace ThirdPartyBroadcast.Consumer.Criteria.HappyFive;

public class HappyFiveCriteria : CalculateCriteria
{
    public string Resources { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_s164_{DbServer}";
}