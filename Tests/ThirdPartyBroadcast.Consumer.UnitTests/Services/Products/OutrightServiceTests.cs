namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria;

public class OutrightServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly OutrightService service;

    public OutrightServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var mapper = Substitute.For<IMapper>();
        service = new OutrightServiceImp(mapper, calculateService, scaleOutSettleThirdPartyService);
    }

    [Fact]
    public async Task Settle_ValidateMoveBetDayIsFailed_ReturnResultError()
    {
        var resultMessage = new ResultMessage { ErrorCode = 1, ErrorMessage = "Error Message" };
        scaleOutSettleThirdPartyService.ValidateMoveBetDay(Arg.Any<string>(), Arg.Any<string>()).Returns(resultMessage);

        var result = await service.Settle(new OutrightRequest());

        Assert.Equal(1, result.ErrorCode);
    }

    [Fact]
    public async Task Settle_ValidateMoveBetDayIsPassed_DoSettle()
    {
        scaleOutSettleThirdPartyService.ValidateMoveBetDay(Arg.Any<string>(), Arg.Any<string>()).Returns(new ResultMessage());
        calculateService.Execute(Arg.Any<OutrightCriteria>()).Returns(new Result());
        var result = await service.Settle(new OutrightRequest());

        Assert.True(result.IsSuccessful);
        await calculateService.Received().Execute(Arg.Any<OutrightCriteria>());
    }
}