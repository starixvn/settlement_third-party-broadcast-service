namespace ThirdPartyBroadcast.Consumer.Services;

public interface RecipientTicketsService
{
    void CreditTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient,
        MatchInfo matchInfos);

    void DepositTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketInfos, Recipient recipient,
        MatchInfo matchInfos);

    void CreditTickets(string actionName, int functionId, IReadOnlyCollection<TicketInfo> ticketInfos, MatchInfo matchInfos);
}

public class RecipientTicketsServiceImp : RecipientTicketsService
{
    private readonly SendMailService sendMailService;

    public RecipientTicketsServiceImp(SendMailService sendMailService)
    {
        this.sendMailService = sendMailService;
    }

    public void CreditTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketsInfo, Recipient recipient, MatchInfo matchInfos)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var mailModel = new NotificationModel
        {
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

    public void DepositTickets(string actionName, IReadOnlyCollection<TicketInfo> ticketsInfo, Recipient recipient, MatchInfo matchInfos)
    {
        if (ticketsInfo.Count < 1)
        {
            return;
        }

        var tickets = SplitTicketByLicensee(ticketsInfo).ToList();

        var mailModel = new NotificationModel
        {
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

    public void CreditTickets(string actionName, int functionId, IReadOnlyCollection<TicketInfo> ticketInfos, MatchInfo matchInfo)
    {
        //if (ticketInfos.Count < 1)
        //{
        //    return;
        //}

        //var tickets = new List<TicketModel> {
        //    new()
        //    {
        //        TicketInfos = ticketInfos.ToList()
        //    }
        //};

        //SendMail(actionName, EmailType.Credit, tickets, matchInfo);
    }

    private void SendMail(string actionName, string type, IReadOnlyCollection<TicketInfo> tickets, MatchInfo matchInfo)
    {
        //matchInfo.TotalTickets = tickets.SelectMany(i => i.TicketInfos).Distinct().Count();

        //var mailModel = new NotificationModel
        //{
        //    MatchInfo = matchInfo,
        //    Action = actionName,
        //    Type = type,
        //    Tickets = tickets
        //};

        //sendMailService.SendMail(mailModel);
    }
}