using Fanex.Data;
using System.Text.Json.Serialization;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class NumberGameCriteria : CalculateCriteria
{
    public int DrawSeq { get; set; }

    public bool IsLive { get; set; }

    public string Action { get; set; }

    public bool IsLoopTempTable { get; set; }

    [JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_s161";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}