namespace ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public class ValidateBetBuilderCriteria : CriteriaBase
{
    public int MatchId { get; set; }

    public bool IsAuto { get; set; }

    public string SelectedSelection { get; set; }

    public override string GetSettingKey() => "NAP_Admin_Settlement_BetBuilder_Validation_Event";

    public override bool IsValid() => MatchId > 0;
}