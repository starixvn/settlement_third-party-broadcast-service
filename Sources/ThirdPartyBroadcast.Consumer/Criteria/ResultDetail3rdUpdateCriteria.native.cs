namespace ThirdPartyBroadcast.Consumer.Criteria;

public class ResultDetail3rdUpdateCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public short SportType { get; set; }

    public string Action { get; set; }

    public int AdminId { get; set; }

    public bool IsAuto { get; set; }

    public string GameResult { get; set; }

    public int ResultId { get; set; }

    public string SelectedResources { get; set; }

    public override string GetSettingKey() => "NAP_Admin_Result3rd_Update";

    public override bool IsValid() => MatchId > 0;
}