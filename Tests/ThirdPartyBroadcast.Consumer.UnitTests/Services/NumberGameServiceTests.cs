using ThirdPartyBroadcast.Criteria.NumberGame;

namespace ThirdPartyBroadcast.UnitTests.Services;

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

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new NumberGameServiceImp(mapper, scaleOutSettleThirdPartyService, calculateService, logDirectlyService);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcess()
    {
        var resultFailed = new Result
        {
            ErrorCode = 99,
        };

        calculateService.ValidationDeployFlag(SpName.NumberGame).Returns(resultFailed);

        var result = await service.Settle(new AutoNumberGameRequest());

        //Assert
        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);
        await scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_DoSettle()
    {
        calculateService.ValidationDeployFlag(SpName.NumberGame).Returns(new Result());

        await service.Settle(new AutoNumberGameRequest());

        //Assert
        await scaleOutSettleThirdPartyService.Received().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
        await calculateService.Received().Execute(Arg.Any<NumberGameCriteria>());
        await logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Settle_ValidatePassedButMatchLive_DoSettleWithoutUpdateMatch()
    {
        calculateService.ValidationDeployFlag(SpName.NumberGame).Returns(new Result());

        await service.Settle(new AutoNumberGameRequest { IsLive = true });

        //Assert
        await scaleOutSettleThirdPartyService.DidNotReceive().UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
        await calculateService.Received().Execute(Arg.Any<NumberGameCriteria>());
    }
}