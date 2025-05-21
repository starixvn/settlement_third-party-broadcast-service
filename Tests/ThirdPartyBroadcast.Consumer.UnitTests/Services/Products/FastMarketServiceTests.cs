using Fanex.Settlement.ScaleOut.Request;
using MassTransit;
using ThirdPartyBroadcast.Consumer.Models.FM;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria;
using ThirdPartyBroadcast.Consumer.Services;

public class FastMarketServiceTests
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly CalculateService calculateService;

    private readonly FastMarketServiceImp service;

    private readonly LogDirectlyService logDirectlyService;

    private readonly MatchService matchService;

    private readonly XmlConvertService xmlConvertService;

    private readonly IPublishEndpoint publishEndpoint;

    public FastMarketServiceTests()
    {
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        calculateService = Substitute.For<CalculateService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        matchService = Substitute.For<MatchService>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        publishEndpoint = Substitute.For<IPublishEndpoint>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new FastMarketServiceImp(scaleOutSettleService, mapper, calculateService, logDirectlyService, matchService, xmlConvertService, publishEndpoint);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcess()
    {
        var resultFailed = new ResultMessage
        {
            ErrorCode = 99
        };

        scaleOutSettleService.ValidateEventAndTicketFastMarket(Arg.Any<EventTicketFastMarketRequest>()).Returns(resultFailed);

        var result = await service.Settle(new FastMarketRequest());

        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);
        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_DoSettle()
    {
        var betTypes = new BetTypeXML
        {
            BetType = []
        };
        scaleOutSettleService.ValidateEventAndTicketFastMarket(Arg.Any<EventTicketFastMarketRequest>()).Returns(new ResultMessage());
        calculateService.Execute(Arg.Any<CalculateCriteria>()).Returns(new Result());
        matchService.GetFastMarketResult(Arg.Any<int>()).Returns(new List<FastMarketResult>());
        xmlConvertService.Deserialize<BetTypeXML>(Arg.Any<string>()).Returns(betTypes);
        await service.Settle(new FastMarketRequest());

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<FastMarketCriteria>());
        await logDirectlyService.Received().WriteLog(Arg.Any<int>(), Arg.Any<string>());
        await logDirectlyService.Received().WriteLog(Arg.Any<InsertLogCriteria>());
    }
}