using Fanex.Data;
using System.Text.Json.Serialization;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class PinGoalCriteria : CalculateCriteria
{
    public string BetStatus { get; set; }

    public bool IsReSettledTickets { get; set; }

    public string SelectedResources { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public bool IsPingoal { get; set; } = true;

    public string BroadcastSessionId { get; } = Guid.NewGuid().ToString();

    [JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_Profile_Push";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}