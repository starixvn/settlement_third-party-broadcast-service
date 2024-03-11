namespace ThirdPartyBroadcast.Consumer.Models;

public class NotificationModel
{
    public string Type { get; set; }

    public string Action { get; set; }

    public string To { get; set; }

    public string Copy { get; set; }

    public int MailId { get; set; }

    public int TotalTickets { get; set; }

    public IEnumerable<TicketInfo> TicketsInfo { get; set; }

    public MatchInfo MatchInfo { get; set; }
}