namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;

public class Match3rdUpdateCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public string SelectedResources { get; set; }

    public string BetStatus { get; set; }

    public string EventStatus { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match3rd_Info_Update";

    public override bool IsValid() => MatchId > 0;
}