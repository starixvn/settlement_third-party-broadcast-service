using Fanex.MailDelivery.Client;
using Fanex.RemotingRef;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services;

public class SendMailServiceTests
{
    private readonly SendMailService service;

    public SendMailServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false, true)
            .Build();
        RefDataTypeContainer
            .ConnectTo(new Uri(configuration.GetSection("Fanex.RemotingRef.Service.Url").Value))
            .FromApplication("Settlement");

        MailClientHandler.StartAsync("appsettings.json");
        service = new SendMailServiceImp();
    }

    [Fact]
    public async Task CreditTickets_SendMailByFunctionId_ReturnEmail()
    {
        var tickets = new List<TicketInfo>
        {
            new()
            {
                CustId = 1,
                SiteName = "Site Name Test",
                TicketType = "Single Ticket",
                TransId = "100123456789",
                Username = "Username 1",
                TransDate = DateTime.Now,
            },
            new()
            {
                CustId = 1,
                SiteName = "Site Name Test",
                TicketType = "Selection of Mix Parlay",
                TransId = "100123456790",
                Username = "Username 2",
                TransDate = DateTime.Now,
            }
        };

        var match = new MatchInfo
        {
            MatchId = 77934025,
            AwayId = 1,
            HomeId = 2,
            EventStatus = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            SportType = 1,
            KickOffTime = DateTime.Now,
            LeagueGroupId = 1,
            LeagueId = 1,
            RefDataService = new RefDataServiceImp(new RemotingRefService())
        };

        var model = new NotificationModel
        {
            Subject = "Virtual Sports",
            Type = EmailType.Credit,
            Action = ActionName.Refund,
            MailId = 103,
            MatchInfo = match,
            TicketsInfo = tickets,
            TotalTickets = tickets.Count
        };

        await service.SendMailByFunctionId(model);
    }

    [Fact]
    public async Task DepositTickets_SendMailByFunctionId_ReturnEmail()
    {
        var ticketsInfo = new List<TicketInfo>
        {
            new()
            {
                CustId = 12345678,
                SiteName = "Site Name 1",
                TicketType = "Single Ticket",
                TransId = "100123456789",
                Username = "Username 1",
                TransDate = DateTime.Now,
            },
            new()
            {
                CustId = 12345679,
                SiteName = "Site Name 1",
                TicketType = "Single Ticket",
                TransId = "100123456790",
                Username = "Username 2",
                TransDate = DateTime.Now,
            },
            new()
            {
                CustId = 12345680,
                SiteName = "Site Name 2",
                TicketType = "Selection of Mix Parlay",
                TransId = "100123456791",
                Username = "Username 1",
                TransDate = DateTime.Now,
            },
            new()
            {
                CustId = 12345681,
                SiteName = "Site Name 2",
                TicketType = "Single Ticket",
                TransId = "100123456792",
                Username = "Username 2",
                TransDate = DateTime.Now,
            },
            new()
            {
                CustId = 12345682,
                SiteName = "Site Name 3",
                TicketType = "Selection of Mix Parlay",
                TransId = "100123456793",
                Username = "member test1238",
                TransDate = DateTime.Now,
            }
        };

        var tickets = SplitTicketByLicensee(ticketsInfo).ToList();
        var match = new MatchInfo
        {
            MatchId = 77934025,
            AwayId = 1,
            HomeId = 2,
            EventStatus = DateTime.Now.ToString(CultureInfo.InvariantCulture),
            SportType = 1,
            KickOffTime = DateTime.Now,
            LeagueGroupId = 1,
            LeagueId = 1,
            RefDataService = new RefDataServiceImp(new RemotingRefService())
        };

        var model = new NotificationModel
        {
            Subject = "Virtual Sports",
            Type = EmailType.Deposit,
            Action = ActionName.Refund,
            MailId = 103,
            MatchInfo = match,
            TicketsInfo = tickets,
            TotalTickets = ticketsInfo.Count()
        };

        await service.SendMailByFunctionId(model);
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