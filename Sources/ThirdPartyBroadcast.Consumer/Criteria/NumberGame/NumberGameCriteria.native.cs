namespace ThirdPartyBroadcast.Consumer.Criteria.NumberGame;

public class NumberGameCriteria : CalculateCriteria
{
    public int DrawSeq { get; set; }

    public bool IsLive { get; set; }

    public string Action { get; set; }

    public bool IsLoop { get; set; }

    public bool IsLoopTempTable { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_s161_{DbServer}";
}