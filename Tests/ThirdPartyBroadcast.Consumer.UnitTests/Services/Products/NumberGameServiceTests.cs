using Fanex.Settlement.ScaleOut.Request;
using Fanex.SiteDbConnectionManager.Client.Models;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class NumberGameServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly IMapper mapper;

    private readonly NumberGameServiceImp service;

    public NumberGameServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        calculateService.Execute(Arg.Any<NumberGameCriteria>(), Arg.Any<SiteDbPendingCommand>()).Returns(new Result());

        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        var repository = Substitute.For<IDynamicRepository>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        mapper = Substitute.For<IMapper>();

        service = new NumberGameServiceImp(mapper, scaleOutSettleThirdPartyService, calculateService, logDirectlyService, repository);
    }

    [Theory]
    [InlineData("CAL")]
    [InlineData("Refund")]
    public async Task Settle_MatchIsNotLive_CallAllInOrderMethod(string action)
    {
        mapper.Map<NumberGameCriteria>(Arg.Any<NumberGameRequest>()).Returns(new NumberGameCriteria
        {
            Action = action
        });

        await service.Settle(new NumberGameRequest { IsLive = false, Action = action });

        Received.InOrder(() =>
        {
            scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
            logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
            calculateService.Received().Execute(Arg.Any<NumberGameCriteria>(), Arg.Any<SiteDbPendingCommand>());
        });
    }

    [Fact]
    public async Task Settle_MatchIsLive_WithoutCallUpdateMatch()
    {
        mapper.Map<NumberGameCriteria>(Arg.Any<NumberGameRequest>()).Returns(new NumberGameCriteria
        {
            Action = ActionName.Calculate
        });

        await service.Settle(new NumberGameRequest { IsLive = true });

        await scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());

        Received.InOrder(() =>
        {
            logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
            calculateService.Received().Execute(Arg.Any<NumberGameCriteria>(), Arg.Any<SiteDbPendingCommand>());
        });
    }
}