using Fanex.Data;
using ThirdPartyBroadcast.Consumer.Helper;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class CalculateCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public int AdminId { get; set; }

    public string Ip { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    [System.Text.Json.Serialization.JsonIgnore, SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey()
    {
        throw new NotImplementedException();
    }

    public override bool IsValid() => MatchId > 0;

    public string ToJson() => System.Text.Json.JsonSerializer.Serialize(this, GetType(), JsonSettings.Options);
}