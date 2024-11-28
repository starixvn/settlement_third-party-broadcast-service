using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;

public class VirtualSportsFiveTicketCriteria : NonQueryCommand
{
    public int MatchId { get; set; }

    public short SportType { get; set; }

    public string ResourceIds { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Set_Match3rd_SendNotify_{DbServer}";

    public override bool IsValid() => MatchId > 0 && !string.IsNullOrEmpty(ResourceIds);
}