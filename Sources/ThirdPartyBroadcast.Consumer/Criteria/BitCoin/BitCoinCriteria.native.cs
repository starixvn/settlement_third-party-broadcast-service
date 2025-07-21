using Fanex.Data;
using System.Text.Json.Serialization;

namespace ThirdPartyBroadcast.Consumer.Criteria.BitCoin;

public class BitCoinCriteria : CalculateCriteria
{
    public string GameResult { get; set; }

    public int ResultId { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    [JsonIgnore, SpParam(Ignored = true)]
    public string SpName { get; set; } = "NAP_Admin_Settlement_s175";

    public override string GetSettingKey() => $"{SpName}_{DbServer}";
}