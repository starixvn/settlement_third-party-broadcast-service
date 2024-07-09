namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria.BitCoin;
using ThirdPartyBroadcast.Consumer.Criteria;

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
    public async Task Settle_IsFailed_ReturnResultError()
    {
        var resultMessage = new Result { ErrorCode = 1, ErrorMessage = "Error Message" };
        calculateService.Execute(Arg.Any<BitCoinCriteria>()).Returns(resultMessage);

        var result = await service.Settle(new BitCoinRequest());

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task Settle_IsSuccessful_DoNotNotify()
    {
        calculateService.Execute(Arg.Any<BitCoinCriteria>()).Returns(new Result());

        var result = await service.Settle(new BitCoinRequest());

        Assert.True(result.IsSuccessful);
        await repository.Received().GetAsync<string>(Arg.Any<Match3rdSelCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<BitCoinUpdateMatchCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<UpdateEventStatusCriteria>());
        await calculateService.Received().Execute(Arg.Any<BitCoinCriteria>());
        await repository.DidNotReceive().ExecuteAsync(Arg.Any<NotifyCriteria>());
    }

    [Theory]
    [InlineData(EventStatus.Completed, ActionName.Calculate)]
    [InlineData("Running", ActionName.Refund)]
    public async Task Settle_ReSettle_ShouldNotify(string eventStatus, string action)
    {
        repository.GetAsync<string>(Arg.Any<Match3rdSelCriteria>()).Returns(eventStatus);
        calculateService.Execute(Arg.Any<BitCoinCriteria>()).Returns(new Result());

        var result = await service.Settle(new BitCoinRequest { Action = action });

        Assert.True(result.IsSuccessful);
        await repository.Received().ExecuteAsync(Arg.Any<BitCoinUpdateMatchCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<UpdateEventStatusCriteria>());
        await calculateService.Received().Execute(Arg.Any<BitCoinCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<NotifyCriteria>());
        await logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }
}