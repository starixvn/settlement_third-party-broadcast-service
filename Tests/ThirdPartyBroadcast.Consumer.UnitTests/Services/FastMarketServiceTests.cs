using Fanex.Settlement.ScaleOut.Models;

namespace ThirdPartyBroadcast.UnitTests.Services;

public class FastMarketServiceTests
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly CalculateService calculateService;

    private readonly FastMarketService service;

    private readonly LogDirectlyService logDirectlyService;

    public FastMarketServiceTests()
    {
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        calculateService = Substitute.For<CalculateService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new FastMarketServiceImp(scaleOutSettleService, mapper, calculateService, logDirectlyService);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcess()
    {
        var resultFailed = new ResultMessage
        {
            ErrorCode = 99,
        };

        scaleOutSettleService.ValidateEventAndTicketFastMarket(Arg.Any<EventTicketFastMarketRequest>()).Returns(resultFailed);

        var result = await service.Settle(new AutoFastMarketRequest());

        //Assert
        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);
        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_DoSettle()
    {
        scaleOutSettleService.ValidateEventAndTicketFastMarket(Arg.Any<EventTicketFastMarketRequest>()).Returns(new ResultMessage());

        await service.Settle(new AutoFastMarketRequest());

        //Assert
        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<FastMarketCriteria>());
        await logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }
}