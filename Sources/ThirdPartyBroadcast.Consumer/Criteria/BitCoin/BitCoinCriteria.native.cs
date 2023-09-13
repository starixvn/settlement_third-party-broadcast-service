namespace ThirdPartyBroadcast.Consumer.Criteria.BitCoin;

public class BitCoinCriteria : CalculateCriteria
{
    public string GameResult { get; set; }

    public int ResultId { get; set; }

    public bool IsAuto { get; set; }

    public string Action { get; set; }

    public override string GetSettingKey() => $"NAP_Admin_Settlement_s175_{DbServer}";
}