using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Criteria.HappyFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class HappyFiveServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly HappyFiveService service;
    private readonly LogDirectlyService logDirectlyService;

    public HappyFiveServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new HappyFiveServiceImp(mapper, calculateService, repository, scaleOutSettleThirdPartyService, logDirectlyService);
    }

    [Fact]
    public async Task Settle_ValidateBeforeSettleIsFailed_ReturnResultError()
    {
        await repository.ExecuteAsync(Arg.Do<ValidateHappyFive>(x =>
        {
            x.ErrCode = 1;
            x.ErrMsg = "Error Message";
        }));

        var result = await service.Settle(new HappyFiveRequest());

        Assert.Equal(1, result.ErrorCode);
    }

    [Fact]
    public async Task Settle_ValidateBeforeSettleIsPassed_DoSettle()
    {
        await repository.ExecuteAsync(Arg.Do<ValidateHappyFive>(x =>
        {
            x.ErrCode = 0;
            x.ErrMsg = string.Empty;
        }));

        var result = await service.Settle(new HappyFiveRequest());

        Received.InOrder(() =>
        {
            scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
            logDirectlyService.Message(Arg.Any<int>());
            logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
            calculateService.Execute(Arg.Any<HappyFiveCriteria>());
        });
    }
}