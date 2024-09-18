namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria;
using Criteria.BitCoin;
using Fanex.Data.Repository;

public class BitCoinServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly BitCoinServiceImp service;

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
        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(new MatchInfo());

        var result = await service.Settle(new BitCoinRequest());

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task Settle_IsSuccessful_DoNotNotify()
    {
        calculateService.Execute(Arg.Any<BitCoinCriteria>()).Returns(new Result());
        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(new MatchInfo());

        var result = await service.Settle(new BitCoinRequest());

        Assert.True(result.IsSuccessful);
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
        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(new MatchInfo { EventStatus = eventStatus });
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