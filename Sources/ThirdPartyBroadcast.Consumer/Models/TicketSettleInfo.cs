namespace ThirdPartyBroadcast.Consumer.Models;

public class TicketSettleInfo
{
    public long TransId { get; set; }

    public short StatusId { get; set; }

    public int? MemberId { get; set; }

    public string MemberUserName { get; set; }

    public int? AgentId { get; set; }

    public string AgentUserName { get; set; }

    public int? MasterId { get; set; }

    public string MasterUserName { get; set; }

    public int? SuperId { get; set; }

    public string SuperUserName { get; set; }

    public string Status => StatusId == StatusType.Running ? TicketType.Running : TicketType.Settled;
}