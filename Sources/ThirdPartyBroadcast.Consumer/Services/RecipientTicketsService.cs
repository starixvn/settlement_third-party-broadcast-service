namespace ThirdPartyBroadcast.Consumer.Services;

public interface RecipientTicketsService
{
    void CreditTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient,
        MatchInfo matchInfos);

    void DepositTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient,
        MatchInfo matchInfos);
}

public class RecipientTicketsServiceImp : RecipientTicketsService
{
    private readonly SendMailService sendMailService;

    public RecipientTicketsServiceImp(SendMailService sendMailService)
    {
        this.sendMailService = sendMailService;
    }

    public void CreditTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient, MatchInfo matchInfos)
    {
        if (ticketInfos.Count > 0)
        {
            var tickets = new List<TicketModel> {
                new()
                {
                    TicketInfos = ticketInfos.ToList()
                }
            };

            matchInfos.TotalTickets = tickets.SelectMany(i => i.TicketInfos).Distinct().Count();

            var mailModel = new NotificationModel
            {
                MatchInfo = matchInfos,
                Action = actionName,
                To = recipient.CreditRecipients,
                Copy = recipient.CopyRecipients,
                Type = EmailType.Credit,
                Ticket = tickets
            };

            sendMailService.SendMail(mailModel);
        }
    }

    public void DepositTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient, MatchInfo matchInfos)
    {
        if (ticketInfos.Count > 0)
        {
            var tickets = SplitTicketByLicensee(ticketInfos).ToList();
            matchInfos.TotalTickets = tickets.SelectMany(i => i.TicketInfos).Distinct().Count();

            var mailModel = new NotificationModel
            {
                MatchInfo = matchInfos,
                Action = actionName,
                To = recipient.DepositRecipients,
                Copy = recipient.CopyRecipients,
                Type = EmailType.Deposit,
                Ticket = tickets
            };

            sendMailService.SendMail(mailModel);
        }
    }

    private static IEnumerable<TicketModel> SplitTicketByLicensee(IReadOnlyCollection<TicketInfo> ticketInfos)
    {
        var distinctLicensees = ticketInfos.Select(ticket => ticket.SiteName).Distinct();

        return distinctLicensees.Select(licensee => new TicketModel
        {
            TicketInfos = ticketInfos.Where(ticket => ticket.SiteName == licensee).ToList()
        });
    }
}