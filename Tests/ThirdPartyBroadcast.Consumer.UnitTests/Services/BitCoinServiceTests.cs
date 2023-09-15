using ThirdPartyBroadcast.Criteria.BitCoin;

namespace ThirdPartyBroadcast.UnitTests.Services;

public class BitCoinServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly CalculateService calculateService;

    private readonly BitCoinService service;

    private readonly LogDirectlyService logDirectlyService;

    public BitCoinServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        calculateService = Substitute.For<CalculateService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new BitCoinServiceImp(repository, mapper, calculateService, logDirectlyService);
    }

    [Fact]
    public async Task Settle_ExistDeployFlag_StopProcess()
    {
        var resultFailed = new Result
        {
            ErrorCode = 99,
        };

        calculateService.ValidationDeployFlag(SpName.BitCoin).Returns(resultFailed);

        var result = await service.Settle(new AutoBitCoinRequest());

        //Assert
        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);
        await repository.DidNotReceive().GetAsync<string>(Arg.Any<BitCoinEventStatusCriteria>());
    }

    [Fact]
    public async Task Settle_SettleSuccess_DoNotNotify()
    {
        calculateService.ValidationDeployFlag(SpName.BitCoin).Returns(new Result());
        calculateService.Execute(Arg.Any<BitCoinCriteria>()).Returns(new Result());

        var result = await service.Settle(new AutoBitCoinRequest());

        //Assert
        Assert.Equal(0, result.ErrorCode);
        await repository.Received().GetAsync<string>(Arg.Any<BitCoinEventStatusCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<BitCoinUpdateMatchCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<UpdateEventStatusCriteria>());
        await calculateService.Received().Execute(Arg.Any<BitCoinCriteria>());
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<NotifyCriteria>());
    }

    [Theory]
    [InlineData(EventStatus.Completed, ActionType.Calculate)]
    [InlineData("Running", ActionType.Refund)]
    public async Task Settle_ReSettle_ShouldNotify(string eventStatus, string action)
    {
        repository.GetAsync<string>(Arg.Any<BitCoinEventStatusCriteria>()).Returns(eventStatus);
        calculateService.ValidationDeployFlag(SpName.BitCoin).Returns(new Result());
        calculateService.Execute(Arg.Any<BitCoinCriteria>()).Returns(new Result());

        var result = await service.Settle(new AutoBitCoinRequest { Action = action });

        //Assert
        Assert.Equal(0, result.ErrorCode);
        await repository.Received().ExecuteAsync(Arg.Any<BitCoinUpdateMatchCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<UpdateEventStatusCriteria>());
        await calculateService.Received().Execute(Arg.Any<BitCoinCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<NotifyCriteria>());
        await logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }
}