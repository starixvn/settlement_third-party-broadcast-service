namespace ThirdPartyBroadcast.Consumer.Models;

public class NotificationModel
{
    public string Type { get; set; }

    public string Action { get; set; }

    public string To { get; set; }

    public string Copy { get; set; }

    public IEnumerable<TicketModel> Ticket { get; set; }

    public MatchInfo MatchInfo { get; set; }
}