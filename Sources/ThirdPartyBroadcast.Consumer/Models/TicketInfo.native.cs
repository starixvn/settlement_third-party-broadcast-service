namespace ThirdPartyBroadcast.Consumer.Models;

public class TicketInfo
{
    public string TransId { get; set; }

    public string TicketType { get; set; }

    public int CustId { get; set; }

    public string Username { get; set; }

    public DateTime TransDate { get; set; }

    public string SiteName { get; set; }

    public bool IsDeposit { get; set; }

    public IEnumerable<TicketInfo> TicketsInfo { get; set; }
}