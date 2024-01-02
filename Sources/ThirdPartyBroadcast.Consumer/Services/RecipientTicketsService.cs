namespace ThirdPartyBroadcast.Consumer.Services;

public interface RecipientTicketsService
{
    void CreditTickets(string actionName, string subject, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient,
        MatchInfo matchInfos);

    void DepositTickets(string actionName, string subject, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient,
        MatchInfo matchInfos);
}

public class RecipientTicketsServiceImp : RecipientTicketsService
{
    private readonly SendMailService sendMailService;

    public RecipientTicketsServiceImp(SendMailService sendMailService)
    {
        this.sendMailService = sendMailService;
    }

    public void CreditTickets(string actionName, string subject, IReadOnlyCollection<TicketInfo> ticketsInfo, Recipient recipient, MatchInfo matchInfos)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var mailModel = new NotificationModel
        {
            Subject = subject,
            MatchInfo = matchInfos,
            Action = actionName,
            To = recipient.CreditRecipients,
            Copy = recipient.CopyRecipients,
            Type = EmailType.Credit,
            TicketsInfo = ticketsInfo,
            TotalTickets = ticketsInfo.Count()
        };

        sendMailService.SendMail(mailModel);
    }

    public void DepositTickets(string actionName, string subject, IReadOnlyCollection<TicketInfo> ticketsInfo, Recipient recipient, MatchInfo matchInfos)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var tickets = SplitTicketByLicensee(ticketsInfo).ToList();

        var mailModel = new NotificationModel
        {
            Subject = subject,
            MatchInfo = matchInfos,
            Action = actionName,
            To = recipient.DepositRecipients,
            Copy = recipient.CopyRecipients,
            Type = EmailType.Deposit,
            TicketsInfo = tickets,
            TotalTickets = ticketsInfo.Count()
        };

        sendMailService.SendMail(mailModel);
    }

    private static IEnumerable<TicketInfo> SplitTicketByLicensee(IReadOnlyCollection<TicketInfo> ticketInfos)
    {
        var distinctLicensees = ticketInfos.Select(ticket => ticket.SiteName).Distinct();

        return distinctLicensees.Select(licensee => new TicketInfo
        {
            TicketsInfo = ticketInfos.Where(ticket => ticket.SiteName == licensee).ToList()
        });
    }
}