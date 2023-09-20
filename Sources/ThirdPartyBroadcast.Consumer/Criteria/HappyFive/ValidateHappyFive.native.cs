namespace ThirdPartyBroadcast.Consumer.Criteria.HappyFive;

public class ValidateHappyFive : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Action { get; set; }

    public int ErrCode { get; set; }

    public string ErrMsg { get; set; }

    public override string GetSettingKey() => "NAP_Admin_Settlement_s164_Validation_Event";

    public override bool IsValid() => MatchId > 0;
}