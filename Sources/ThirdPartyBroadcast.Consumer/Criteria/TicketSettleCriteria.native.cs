using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class TicketSettleCriteria : CriteriaBase
{
    public int? MatchId { get; set; }

    public string TransIDs { get; set; }

    public short SportType { get; set; }

    [SpParam(Ignored = true)]
    public string DbServer { get; set; }

    public override string GetSettingKey() => $"NAP_Set_VS_TicketInfo_Sel_{DbServer}";

    public override bool IsValid() => MatchId > 0 || !string.IsNullOrWhiteSpace(TransIDs);
}