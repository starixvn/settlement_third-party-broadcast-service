using Fanex.Settlement.ScaleOut.Request;
using Settlement.Business.Core.Models;
using ThirdPartyBroadcast.Consumer.Criteria.HappyFive;
using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;
using Result = ThirdPartyBroadcast.Consumer.Models.Result;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class HappyFiveServiceTests
{
    private readonly IDynamicRepository repository;

    private readonly SettleSitesService settleSitesService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly Settlement.Business.Services.MatchService matchService;

    private readonly HappyFiveService service;

    private readonly LogDirectlyService logDirectlyService;

    public HappyFiveServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        settleSitesService = Substitute.For<SettleSitesService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        matchService = Substitute.For<Settlement.Business.Services.MatchService>();

        service = new HappyFiveServiceImp(settleSitesService, repository, scaleOutSettleThirdPartyService, logDirectlyService, matchService);
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

        var resources = new List<Resources>();
        var balls = new List<Ball>();

        matchService.GetResourceHappy5(Arg.Any<int>(), Arg.Any<string>()).Returns((resources, balls));
        settleSitesService.Settle(Arg.Any<HappyFiveRequest>(), Arg.Any<List<Resources>>()).Returns(new Result());
        await service.Settle(new HappyFiveRequest());

        Received.InOrder(() =>
        {
            scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(Arg.Any<UpdateMatchInfo3RdRequest>());
            logDirectlyService.Message(Arg.Any<int>());
            logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());
            settleSitesService.Settle(Arg.Any<HappyFiveRequest>(), Arg.Any<List<Resources>>());
        });
    }
}