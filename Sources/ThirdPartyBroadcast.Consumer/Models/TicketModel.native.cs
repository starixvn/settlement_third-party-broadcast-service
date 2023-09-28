namespace ThirdPartyBroadcast.Consumer.Models;

public class TicketModel
{
    public string SiteName => TicketInfos[0].SiteName;

    public int TotalTicket => TicketInfos.Count;

    public List<TicketInfo> TicketInfos { get; set; }
}