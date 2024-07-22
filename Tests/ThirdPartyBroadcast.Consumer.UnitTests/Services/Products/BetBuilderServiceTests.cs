using ScaleOutRequest = Fanex.Settlement.ScaleOut.Request;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria;
using Requests;

public class BetBuilderServiceTests
{
    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly CalculateService calculateService;

    private readonly XmlConvertService xmlConvertService;

    private readonly IDynamicRepository repository;

    private readonly BetBuilderService service;

    private readonly LogDirectlyService logDirectlyService;

    public BetBuilderServiceTests()
    {
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        calculateService = Substitute.For<CalculateService>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        repository = Substitute.For<IDynamicRepository>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new BetBuilderServiceImp(scaleOutSettleThirdPartyService, scaleOutSettleService, calculateService,
            mapper, xmlConvertService, repository, logDirectlyService);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcessAndReturn()
    {
        var resultFailed = new ResultMessage
        {
            ErrorCode = 1
        };
        scaleOutSettleThirdPartyService.ValidateBetBuilder(Arg.Any<ScaleOutRequest.BetBuilderRequest>())
            .Returns((resultFailed, new InvalidMarkets("1234", "5678")));

        var result = await service.Settle(new BetBuilderRequest());

        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);

        await calculateService.DidNotReceive().Execute(Arg.Any<BetBuilderCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassedAndNotExistInvalidResult_SettleResult()
    {
        var resources = new ResourcesMarketModel
        {
            Items = [new ResourceMarketItemModel { MarketId = "1" }]
        };

        scaleOutSettleThirdPartyService.ValidateBetBuilder(Arg.Any<ScaleOutRequest.BetBuilderRequest>())
            .Returns((new ResultMessage(), new InvalidMarkets(string.Empty, string.Empty)));

        xmlConvertService.Deserialize<ResourcesMarketModel>(Arg.Any<string>()).Returns(resources);

        xmlConvertService.Serialize(Arg.Any<ResourcesMarketModel>()).Returns("test");

        await service.Settle(new BetBuilderRequest());

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<ScaleOutRequest.UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<BetBuilderCriteria>());
    }

    [Theory]
    [InlineData(ActionName.Calculate)]
    [InlineData(ActionName.Refund)]
    public async Task Settle_ValidatePassedAndExistInvalidResult_SettleCorrectResult(string action)
    {
        var resources = new ResourcesMarketModel
        {
            Items =
            [
                new ResourceMarketItemModel { MarketId = "123" },
                new ResourceMarketItemModel { MarketId = "234" }
            ]
        };

        var request = new BetBuilderRequest
        {
            Action = action,
            Ip = "1",
            AdminId = 1,
            MatchId = 1234
        };

        scaleOutSettleThirdPartyService.ValidateBetBuilder(Arg.Any<ScaleOutRequest.BetBuilderRequest>())
            .Returns((new ResultMessage(), new InvalidMarkets("123", "5678")));
        xmlConvertService.Deserialize<ResourcesMarketModel>(Arg.Any<string>()).Returns(resources);
        xmlConvertService.Serialize(Arg.Any<ResourcesMarketModel>()).Returns("2");
        calculateService.Execute(Arg.Any<BetBuilderCriteria>()).Returns(new Result());

        await service.Settle(request);

        await repository.Received(2).ExecuteAsync(Arg.Any<WriteLogCriteria>());
        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<ScaleOutRequest.UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<BetBuilderCriteria>());
    }

    [Theory]
    [InlineData(ActionName.Calculate)]
    [InlineData(ActionName.Refund)]
    public async Task Settle_ValidatePassedAndExistResultNull_SettleCorrectResult(string action)
    {
        var resources = new ResourcesMarketModel
        {
            Items =
            [
                new ResourceMarketItemModel { MarketId = "123" },
                new ResourceMarketItemModel { MarketId = "234" },
            ]
        };

        var request = new BetBuilderRequest
        {
            Action = action,
            Ip = "1",
            AdminId = 1,
            MatchId = 1234
        };

        scaleOutSettleThirdPartyService.ValidateBetBuilder(Arg.Any<ScaleOutRequest.BetBuilderRequest>())
            .Returns((new ResultMessage(), new InvalidMarkets("123", "234")));

        xmlConvertService.Deserialize<ResourcesMarketModel>(request.Resources).Returns(resources);

        xmlConvertService.Serialize(Arg.Any<ResourcesMarketModel>()).Returns("2");
        calculateService.Execute(Arg.Any<BetBuilderCriteria>()).Returns(new Result());

        await service.Settle(request);

        await repository.Received(2).ExecuteAsync(Arg.Any<WriteLogCriteria>());
        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<ScaleOutRequest.UpdateMatchInfoRequest>());
        await calculateService.DidNotReceive().Execute(Arg.Any<BetBuilderCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassedButAllStepAfterReturnError_SettleCorrectResult()
    {
        var resources = new ResourcesMarketModel
        {
            Items =
            [
                new ResourceMarketItemModel { MarketId = "123" },
                new ResourceMarketItemModel { MarketId = "234" }
            ]
        };

        var request = new BetBuilderRequest
        {
            Ip = "1",
            AdminId = 1,
            MatchId = 1234
        };

        var resultFailed = new Result
        {
            ErrorCode = 1,
            ErrorMessage = "error"
        };

        const string message = "error";

        scaleOutSettleThirdPartyService.ValidateBetBuilder(Arg.Any<ScaleOutRequest.BetBuilderRequest>())
            .Returns((new ResultMessage(), new InvalidMarkets("123", "5678")));
        xmlConvertService.Deserialize<ResourcesMarketModel>(Arg.Any<string>()).Returns(resources);
        xmlConvertService.Serialize(Arg.Any<ResourcesMarketModel>()).Returns("2");
        calculateService.Execute(Arg.Any<BetBuilderCriteria>()).Returns(resultFailed);

        var result = await service.Settle(request);

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<ScaleOutRequest.UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<BetBuilderCriteria>());
        await logDirectlyService.WriteLog(Arg.Any<int>(), Arg.Any<string>());

        Assert.Equal(message, result.ErrorMessage);
    }
}