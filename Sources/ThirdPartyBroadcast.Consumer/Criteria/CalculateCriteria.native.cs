using Fanex.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using ThirdPartyBroadcast.Consumer.Helper;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class CalculateCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string Ip { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    [JsonIgnore, SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey()
    {
        throw new NotImplementedException();
    }

    public override bool IsValid() => MatchId > 0;

    public string ToJson() => JsonSerializer.Serialize(this, GetType(), JsonSettings.Options);
}