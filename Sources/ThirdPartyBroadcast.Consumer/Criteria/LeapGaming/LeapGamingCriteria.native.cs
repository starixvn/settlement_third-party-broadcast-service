using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

public class LeapGamingCriteria : CalculateCriteria
{
    public string Action { get; set; }

    public string SelectedResources { get; set; }

    [System.Text.Json.Serialization.JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_s178";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}