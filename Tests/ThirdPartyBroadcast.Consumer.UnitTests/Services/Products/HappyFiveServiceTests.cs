using Settlement.Business.Core.Models;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.HappyFive;
using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;
using Result = ThirdPartyBroadcast.Consumer.Models.Result;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class HappyFiveServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly SettleSitesService settleSitesService;

    private readonly Settlement.Business.Services.MatchService matchService;

    private readonly HappyFiveServiceImp service;

    private readonly XmlConvertService xmlConvertService;

    private readonly MatchService broadcastMatchService;

    public HappyFiveServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        settleSitesService = Substitute.For<SettleSitesService>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        matchService = Substitute.For<Settlement.Business.Services.MatchService>();
        broadcastMatchService = Substitute.For<MatchService>();
        service = new HappyFiveServiceImp(settleSitesService, repository, broadcastMatchService, matchService, xmlConvertService);
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
    public async Task Settle_ValidateBeforeSettleIsPassed_DoCalc()
    {
        await repository.ExecuteAsync(Arg.Do<ValidateHappyFive>(x =>
        {
            x.ErrCode = 0;
            x.ErrMsg = string.Empty;
        }));

        var resources = new List<Resources>();
        var balls = new List<Ball>();
        var request = new HappyFiveRequest
        {
            Action = ActionName.Calculate
        };

        matchService.GetResourceHappy5(Arg.Any<int>(), Arg.Any<string>()).Returns((resources, balls));
        settleSitesService.Settle(Arg.Any<HappyFiveRequest>(), Arg.Any<List<Resources>>()).Returns(new Result());
        await service.Settle(request);

        Received.InOrder(() =>
        {
            broadcastMatchService.UpdateMatchInfoHappyFive(Arg.Any<HappyFiveRequest>());
            broadcastMatchService.WriteLog(Arg.Any<int>());
            matchService.GetResourceHappy5(Arg.Any<int>(), Arg.Any<string>());
            settleSitesService.Settle(Arg.Any<HappyFiveRequest>(), Arg.Any<List<Resources>>());
            repository.ExecuteAsync(Arg.Any<WriteLogCriteria>());
        });

        await repository.DidNotReceive().ExecuteAsync(Arg.Any<UpdateBallsCriteria>());
        xmlConvertService.DidNotReceive().Serialize(Arg.Any<Resources>());
    }

    [Fact]
    public async Task Settle_ValidateBeforeSettleIsPassed_DoRefund()
    {
        await repository.ExecuteAsync(Arg.Do<ValidateHappyFive>(x =>
        {
            x.ErrCode = 0;
            x.ErrMsg = string.Empty;
        }));

        var resources = new List<Resources>();
        var balls = new List<Ball>();
        var request = new HappyFiveRequest
        {
            Action = ActionName.Refund
        };

        matchService.GetResourceHappy5(Arg.Any<int>(), Arg.Any<string>()).Returns((resources, balls));
        settleSitesService.Settle(Arg.Any<HappyFiveRequest>(), Arg.Any<List<Resources>>()).Returns(new Result());
        await service.Settle(request);

        Received.InOrder(() =>
        {
            broadcastMatchService.UpdateMatchInfoHappyFive(Arg.Any<HappyFiveRequest>());
            broadcastMatchService.WriteLog(Arg.Any<int>());
            matchService.GetResourceHappy5(Arg.Any<int>(), Arg.Any<string>());
            settleSitesService.Settle(Arg.Any<HappyFiveRequest>(), Arg.Any<List<Resources>>());
            repository.ExecuteAsync(Arg.Any<UpdateBallsCriteria>());
            repository.ExecuteAsync(Arg.Any<WriteLogCriteria>());
        });
    }
}