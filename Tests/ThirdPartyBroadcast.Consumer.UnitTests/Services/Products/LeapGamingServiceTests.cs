using Fanex.RemotingRef;
using ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class LeapGamingServiceTests
{
    private readonly CalculateService calculateService;

    private readonly MatchService matchService;

    private readonly LeapGamingService service;

    private readonly AnnouncementService announcementService;

    public LeapGamingServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        var repository = Substitute.For<IDynamicRepository>();
        matchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();
        var recipientMessageService = Substitute.For<RecipientMessageService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        service = new LeapGamingServiceImp(mapper, calculateService, repository, matchService, announcementService, recipientMessageService);
    }

    [Fact]
    public async Task Settle_ScoreIncorrectFormat_StopProcessAndReturn()
    {
        var result = await service.Settle(new LeapGamingRequest { SelectedResources = "<Resources> <score Id=\"00\" Home=\"1\" Away=\"1\"/> <score Id=\"01\" Home=\"1\" Away=\"1\"/></Resources>" });

        Assert.True(result.ErrorCode > 0);
        await calculateService.DidNotReceive().Execute(Arg.Any<LeapGamingCriteria>());
    }

    [Fact]
    public async Task Settle_ValidationPassed_InvokeAllMethod()
    {
        var matchInfo = new MatchInfo
        {
            MatchId = 1,
            RefDataService = new RefDataServiceImp(new RemotingRefService())
        };
        matchService.GetMatchInfo(Arg.Any<int>()).Returns(matchInfo);
        calculateService.Execute(Arg.Any<LeapGamingCriteria>()).Returns(new Result());

        await service.Settle(new LeapGamingRequest
        {
            Action = "CAL",
            MatchId = 1,
            SelectedResources = "<Resources> <Resource Id=\"00\" Home=\"1\" Away=\"1\"/> <Resource Id=\"01\" Home=\"1\" Away=\"1\"/></Resources>"
        });

        Received.InOrder(() =>
        {
            matchService.GetMatchInfo(Arg.Any<int>());
            matchService.GetTicketsByAction(Arg.Any<int>(), Arg.Any<string>());
            matchService.UpdateMatchInfoLeapGaming(Arg.Any<LeapGamingRequest>());
            matchService.WriteLogLeapGaming(Arg.Any<int>());
            calculateService.Execute(Arg.Any<LeapGamingCriteria>());
            announcementService.Handle(Arg.Any<List<TicketSettleInfo>>(), Arg.Any<MatchInfo>(), Arg.Any<int>());
        });
    }
}