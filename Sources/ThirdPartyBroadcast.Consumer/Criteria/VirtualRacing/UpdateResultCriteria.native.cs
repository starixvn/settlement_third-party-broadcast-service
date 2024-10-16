namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualRacing;

public class UpdateResultCriteria : NonQueryCommand
{
    public int EventId { get; set; }

    public string EventStatus { get; set; } = Constants.EventStatus.Completed;

    public string BetStatus { get; set; }

    public override string GetSettingKey() => "NAP_Admin_VS_Betradar_HorseDog_Result_Upd";

    public override bool IsValid() => EventId > 0;
}