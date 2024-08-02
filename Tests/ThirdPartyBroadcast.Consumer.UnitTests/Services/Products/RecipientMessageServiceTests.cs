using Microsoft.Extensions.Options;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class RecipientMessageServiceTests
{
    private readonly MatchService matchService;

    private readonly SendMailService sendMailService;

    private readonly RecipientMessageService service;

    public RecipientMessageServiceTests()
    {
        matchService = Substitute.For<MatchService>();
        sendMailService = Substitute.For<SendMailService>();
        var appSettingsKey = new AppSettingsKey { MailFunction = new MailFunction { VirtualSport = new MailInfo() } };
        var options = Options.Create(appSettingsKey);

        service = new RecipientMessageServiceImp(matchService, sendMailService, options);
    }

    [Fact]
    public async Task Message_IsNotReSettleOrRefund_Returns()
    {
        var match = new MatchInfo
        {
            EventStatus = EventStatus.Running
        };

        matchService.IsReSettleOrRefund(Arg.Any<string>(), Arg.Any<string>()).Returns(false);
        await service.Message(ActionName.ReSettle, match);

        await sendMailService.DidNotReceive().SendMailByFunctionId(Arg.Any<NotificationModel>());
    }

    [Fact]
    public async Task Message_IsReSettleOrRefundAndNoTicket_Returns()
    {
        var match = new MatchInfo
        {
            MatchId = 1,
            EventStatus = EventStatus.Running
        };
        const string action = ActionName.Refund;
        IEnumerable<TicketInfo> creditTickets = new List<TicketInfo>();
        IEnumerable<TicketInfo> depositTickets = new List<TicketInfo>();

        matchService.IsReSettleOrRefund(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        matchService.GetAllTicketsVirtualSports(Arg.Any<int>()).Returns((creditTickets, depositTickets));

        await service.Message(action, match);

        await sendMailService.DidNotReceive().SendMailByFunctionId(Arg.Any<NotificationModel>());
    }

    [Fact]
    public async Task Message_IsReSettleOrRefundAndHaveTickets_CallSendMailByFunctionIdMethod()
    {
        var match = new MatchInfo
        {
            MatchId = 1,
            EventStatus = EventStatus.Running
        };
        const string action = ActionName.ReSettle;
        IEnumerable<TicketInfo> creditTickets = new List<TicketInfo> { new() { CustId = 1 } };
        IEnumerable<TicketInfo> depositTickets = new List<TicketInfo> { new() { CustId = 2 } };

        matchService.IsReSettleOrRefund(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        matchService.GetAllTicketsVirtualSports(Arg.Any<int>()).Returns((creditTickets, depositTickets));

        await service.Message(action, match);

        await sendMailService.Received(2).SendMailByFunctionId(Arg.Any<NotificationModel>());
    }
}