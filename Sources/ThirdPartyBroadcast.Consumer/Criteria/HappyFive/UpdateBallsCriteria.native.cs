namespace ThirdPartyBroadcast.Consumer.Criteria.HappyFive;

public class UpdateBallsCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string Resources { get; set; }

    public int SportType => 164;

    public override string GetSettingKey() => "NAP_Admin_Result_Update_BingoBall";

    public override bool IsValid() => MatchId > 0;
}