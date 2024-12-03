using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class PinGoalServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly PinGoalService service;

    public PinGoalServiceTests()
    {
        var logDirectlyService = Substitute.For<LogDirectlyService>();
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new PinGoalServiceImp(scaleOutSettleService, mapper, calculateService, logDirectlyService);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcess()
    {
        var resultFailed = new ResultMessage
        {
            ErrorCode = 99
        };

        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(resultFailed);

        var result = await service.Settle(new PinGoalRequest());

        //Assert
        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);
        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_DoSettle()
    {
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage());

        await service.Settle(new PinGoalRequest());

        //Assert
        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<PinGoalCriteria>());
    }
}