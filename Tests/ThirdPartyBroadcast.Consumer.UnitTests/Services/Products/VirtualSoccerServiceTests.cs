namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria.VirtualSoccer;
using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualSoccerServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly IDynamicRepository repository;

    private readonly VirtualSoccerService service;

    private readonly LogDirectlyService logDirectlyService;

    private readonly AnnouncementService announcementService;

    private readonly TicketSettleService ticketSettleService;

    public VirtualSoccerServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        repository = Substitute.For<IDynamicRepository>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        announcementService = Substitute.For<AnnouncementService>();
        ticketSettleService = Substitute.For<TicketSettleService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new VirtualSoccerServiceImp(scaleOutSettleThirdPartyService, repository, mapper, calculateService, logDirectlyService, announcementService, ticketSettleService);
    }

    [Theory]
    [InlineData(ActionType.Calculate, 1, 1, SpName.VirtualSports + "-CAL")]
    [InlineData(ActionType.Refund, null, null, SpName.VirtualSports + "-REFUND")]
    public async Task Settle_ValidatePassed_DoSettle(string action, int? home, int? away, string command)
    {
        var request = new VirtualSoccerRequest
        {
            Action = action,
            Fta = away,
            Fth = home
        };

        await service.Settle(request);

        //Assert
        await calculateService.Received().Execute(Arg.Any<VirtualSoccerCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<VrSoccerUpdateMatchCriteria>());
        await scaleOutSettleThirdPartyService.Received()
            .UpdateMatchInfo3rd(Arg.Is<UpdateMatchInfo3RdRequest>(item => item.Command == command && item.FTAway == away && item.FTHome == home));
        await logDirectlyService.Received().WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }
}