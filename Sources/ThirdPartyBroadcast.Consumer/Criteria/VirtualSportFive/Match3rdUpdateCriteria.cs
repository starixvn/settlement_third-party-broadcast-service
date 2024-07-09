namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;

public class Match3rdUpdateCriteria : NonQueryCommand
{
    public int Match3rdId { get; set; }
    public string Action { get; set; }
    public string SelectedResources { get; set; }

    public override string GetSettingKey() => "NAP_Set_Match3rd_Info_Update";

    public override bool IsValid() => Match3rdId > 0;
}