using Fanex.Data;

namespace ThirdPartyBroadcast.Consumer.Criteria;

public class TicketSettleCriteria : TicketInfoSelBase
{
    public override string GetSettingKey() => $"NAP_Set_VS_TicketInfo_Sel_{DbServer}";

    public override bool IsValid() => MatchId > 0 || !string.IsNullOrWhiteSpace(TransIDs);
}