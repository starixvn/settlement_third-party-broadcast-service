using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.LeapGaming;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class LeapGamingServiceTests
{
    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly IDynamicRepository repository;

    private readonly LeapGamingService service;

    public LeapGamingServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        repository = Substitute.For<IDynamicRepository>();
        var sendMailService = Substitute.For<SendMailService>();
        var matchService = Substitute.For<MatchService>();
        var announcementService = Substitute.For<AnnouncementService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        service = new LeapGamingServiceImp(mapper, calculateService, logDirectlyService, repository, sendMailService, matchService, announcementService);
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
}