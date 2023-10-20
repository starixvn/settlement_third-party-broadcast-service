using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class LeapGamingServiceTests
{
    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly IDynamicRepository repository;

    private readonly LeapGamingService service;

    private readonly RecipientTicketsService recipientTicketsService;
    private readonly TicketSettleService ticketSettleService;
    private readonly AnnouncementService announcementService;

    public LeapGamingServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        repository = Substitute.For<IDynamicRepository>();
        recipientTicketsService = Substitute.For<RecipientTicketsService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        service = new LeapGamingServiceImp(mapper, calculateService, logDirectlyService, repository, recipientTicketsService, ticketSettleService, announcementService);
    }

    [Fact]
    public async Task Settle_ScoreIncorrectFormat_StopProcessAndReturn()
    {
        var result = await service.Settle(new LeapGamingRequest { SelectedResources = "<Resources> <score Id=\"00\" Home=\"1\" Away=\"1\"/> <score Id=\"01\" Home=\"1\" Away=\"1\"/></Resources>" });

        Assert.True(result.ErrorCode > 0);
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<UpdateMatchCriteria>());
    }

    [Fact]
    public async Task Settle_ValidationPassed_UpdateAndSettleMatch()
    {
        repository.GetAsync<MatchInfo>(Arg.Any<LeapGamingGetMatchInfoCriteria>()).Returns(new MatchInfo());
        calculateService.Execute(Arg.Any<LeapGamingCriteria>()).Returns(new Result());

        var fakeData = new IEnumerable<object>[3];
        fakeData[0] = new List<MatchInfo> { new() };
        fakeData[1] = new List<TicketInfo> { new() { CustId = 1 } };
        fakeData[2] = new List<TicketInfo> { new() { CustId = 1 } };
        repository
            .FetchMultipleAsync(Arg.Any<LeapGamingTicketCriteria>(),
                Arg.Any<Type[]>()).Returns(fakeData);

        await service.Settle(new LeapGamingRequest
        {
            Action = "CAL",
            MatchId = 1,
            SelectedResources = "<Resources> <Resource Id=\"00\" Home=\"1\" Away=\"1\"/> <Resource Id=\"01\" Home=\"1\" Away=\"1\"/></Resources>"
        });

        await repository.Received().ExecuteAsync(Arg.Any<UpdateMatchCriteria>());
        await logDirectlyService.Received().WriteLog(1, string.Empty);
        await calculateService.Received().Execute(Arg.Any<LeapGamingCriteria>());
    }

    [Theory]
    [InlineData("CAL", "Completed", "<Resources> <Resource Id=\"00\" Home=\"1\" Away=\"1\"/> <Resource Id=\"01\" Home=\"1\" Away=\"1\"/></Resources>", 1)]
    [InlineData("CAL", "Running", "<Resources> <Resource Id=\"00\" Home=\"1\" Away=\"1\"/> <Resource Id=\"01\" Home=\"1\" Away=\"1\"/></Resources>", 0)]
    [InlineData("REFUND", "Running", "<Resources> <Resource Id=\"00\" Home=\"\" Away=\"\"/> <Resource Id=\"01\" Home=\"\" Away=\"\"/></Resources>", 1)]
    public async Task Settle_ActionReCalOrRefund_SendMail(string action, string eventStatus, string selectedResources, int mailCount)
    {
        repository.GetAsync<MatchInfo>(Arg.Any<LeapGamingGetMatchInfoCriteria>()).Returns(new MatchInfo() { EventStatus = eventStatus });
        calculateService.Execute(Arg.Any<LeapGamingCriteria>()).Returns(new Result());

        var credit = new List<TicketInfo> { new() { CustId = 1 } };
        var deposit = new List<TicketInfo> { new() { CustId = 1 } };
        repository
            .FetchMultipleAsync<TicketInfo, TicketInfo>(Arg.Any<LeapGamingTicketCriteria>()).Returns((credit, deposit));

        await service.Settle(new LeapGamingRequest
        {
            Action = action,
            MatchId = 1,
            SelectedResources = selectedResources
        });

        recipientTicketsService.Received(mailCount).CreditTickets(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<TicketInfo>>(), Arg.Any<Recipient>(), Arg.Any<MatchInfo>());
        recipientTicketsService.Received(mailCount).DepositTickets(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<TicketInfo>>(), Arg.Any<Recipient>(), Arg.Any<MatchInfo>());
    }
}