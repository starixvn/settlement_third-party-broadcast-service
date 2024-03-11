using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class NumberGameServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly NumberGameService service;
    private readonly LogDirectlyService logDirectlyService;

    public NumberGameServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        var repository = Substitute.For<IDynamicRepository>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new NumberGameServiceImp(mapper, scaleOutSettleThirdPartyService, calculateService, logDirectlyService, repository);
    }

    [Theory]
    [InlineData("CAL")]
    [InlineData("Refund")]
    public async Task Settle_MatchIsNotLive_CallAllInOrderMethod(string action)
    {
        await service.Settle(new NumberGameRequest { IsLive = false, Action = action });

        Received.InOrder(() =>
        {
            scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
            logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
            calculateService.Received().Execute(Arg.Any<NumberGameCriteria>());
        });
    }

    [Fact]
    public async Task Settle_MatchIsLive_WithoutCallUpdateMatch()
    {
        await service.Settle(new NumberGameRequest { IsLive = true });

        await scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());

        Received.InOrder(() =>
        {
            logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
            calculateService.Received().Execute(Arg.Any<NumberGameCriteria>());
        });
    }
}