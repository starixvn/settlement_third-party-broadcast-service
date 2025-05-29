using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Models.FM;
using ResultMessage = Fanex.Settlement.ScaleOut.Models.ResultMessage;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.FastMarket;

using Criteria;
using ThirdPartyBroadcast.Consumer.Services;
using ThirdPartyBroadcast.Consumer.Services.Products.FastMarket;
using SiteDbModels = Fanex.SiteDbConnectionManager.Client.Models;

public class FastMarketServiceTests
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly CalculateService calculateService;

    private readonly FastMarketServiceImp service;

    private readonly LogDirectlyService logDirectlyService;

    private readonly MatchService matchService;

    private readonly XmlConvertService xmlConvertService;

    public FastMarketServiceTests()
    {
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        calculateService = Substitute.For<CalculateService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        matchService = Substitute.For<MatchService>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        var fmResultAmendmentService = Substitute.For<FastMarketResultAmendmentService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new FastMarketServiceImp(scaleOutSettleService, mapper, calculateService, logDirectlyService, fmResultAmendmentService);
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
        calculateService.Execute(Arg.Any<CalculateCriteria>(), Arg.Any<SiteDbModels.SiteDbPendingCommand>()).Returns(new Result());
        matchService.GetFastMarketResult(Arg.Any<int>()).Returns(new List<FastMarketResult>());
        xmlConvertService.Deserialize<BetTypeXML>(Arg.Any<string>()).Returns(betTypes);
        await service.Settle(new FastMarketRequest());

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<FastMarketCriteria>(), Arg.Any<SiteDbModels.SiteDbPendingCommand>());
        await logDirectlyService.Received().WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }
}