namespace ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

public class LeapGamingCriteria : CalculateCriteria
{
    public string Action { get; set; }

    public string SelectedResources { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_s178_{DbServer}";
}