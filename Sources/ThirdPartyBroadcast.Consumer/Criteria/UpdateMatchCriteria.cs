namespace ThirdPartyBroadcast.Consumer.Criteria;

public class UpdateMatchCriteria : INonQueryCommand
{
    public int MatchId { get; set; }

    public string Action { get; set; }

    public string Command { get; set; }

    public string SelectedResources { get; set; }

    public string GetSettingKey() => "NAP_Admin_Match_Update";

    public bool IsValid() => MatchId > 0;
}