using Fanex.Settlement.ScaleOut.Models;
using ThirdPartyBroadcast.Criteria.OutRight;

namespace ThirdPartyBroadcast.UnitTests.Services;

public class OutRightServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly OutrightService service;

    public OutRightServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new OutrightServiceImp(mapper, calculateService, scaleOutSettleThirdPartyService);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task Settle_ValidateFailed_StopProcess(int validateDeployFlag, int validateMoveBetDay)
    {
        calculateService.ValidationDeployFlag(SpName.OutRight)
            .Returns(new Result { ErrorCode = validateDeployFlag });
        scaleOutSettleThirdPartyService.ValidateMoveBetDay(Arg.Any<string>(), "outright").Returns(new ResultMessage { ErrorCode = validateMoveBetDay });

        var result = await service.Settle(new AutoOutrightRequest());

        Assert.Equal(1, result.ErrorCode);
        await calculateService.DidNotReceive().Execute(Arg.Any<OutRightCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_DoSettle()
    {
        calculateService.ValidationDeployFlag(SpName.OutRight)
            .Returns(new Result());
        scaleOutSettleThirdPartyService.ValidateMoveBetDay(Arg.Any<string>(), "outright").Returns(new ResultMessage());

        await service.Settle(new AutoOutrightRequest());

        //Assert
        await calculateService.Received().Execute(Arg.Any<OutRightCriteria>());
    }
}