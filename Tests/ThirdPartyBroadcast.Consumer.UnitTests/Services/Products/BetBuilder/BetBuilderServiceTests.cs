using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;
using ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.BetBuilder;

public class BetBuilderServiceTests
{
    private readonly BetBuilderGenerateService betBuilderGenerateService;

    private readonly CalculateService calculateService;

    private readonly BetBuilderServiceImp service;
    private readonly XmlConvertService xmlConvertService;
    private readonly LogDirectlyService logDirectlyService;
    private readonly IBetBuilderResultAmendmentService betBuilderResultAmendmentService;
    private readonly MatchService matchService;

    public BetBuilderServiceTests()
    {
        betBuilderGenerateService = Substitute.For<BetBuilderGenerateService>();
        calculateService = Substitute.For<CalculateService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();

        var mapper = Substitute.For<IMapper>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        betBuilderResultAmendmentService = Substitute.For<IBetBuilderResultAmendmentService>();
        matchService = Substitute.For<MatchService>();

        // Setup mock mapper to return proper mapped objects
        mapper.Map<BetBuilderCriteria>(Arg.Any<BetBuilderRequest>()).Returns(new BetBuilderCriteria());

        service = new BetBuilderServiceImp(calculateService, mapper, betBuilderGenerateService, xmlConvertService, logDirectlyService, betBuilderResultAmendmentService, matchService);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcessAndReturn()
    {
        var results = new List<Result>
        {
            new()
            {
                ErrorCode = 1,
                ErrorMessage = "Error"
            }
        };
        var reason = new List<ReasonModel>();
        betBuilderGenerateService.ValidationBeforeSettle(Arg.Any<BetBuilderRequest>()).Returns((results, reason));

        var result = await service.Settle(new BetBuilderRequest());

        Assert.True(result.ErrorCode > 0);

        await calculateService.DidNotReceive().Execute(Arg.Any<BetBuilderCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassedReasonErrorEmptyAndSelectedMarketEmpty_CallRightSomeMethodAndNotCallExecute()
    {
        var results = new List<Result>();
        var reason = new List<ReasonModel>();

        betBuilderGenerateService.ValidationBeforeSettle(Arg.Any<BetBuilderRequest>()).Returns((results, reason));
        betBuilderGenerateService.GetInfoMarket(Arg.Any<BetBuilderRequest>()).Returns(string.Empty);
        var result = await service.Settle(new BetBuilderRequest());

        Received.InOrder(() =>
        {
            betBuilderGenerateService.UpdateMatchInfo(Arg.Any<BetBuilderRequest>(), Arg.Any<string>());
            betBuilderGenerateService.WriteLog(Arg.Any<BetBuilderRequest>());
        });

        Assert.Equal(0, result.ErrorCode);
        await calculateService.DidNotReceive().Execute(Arg.Any<BetBuilderCriteria>());
        betBuilderGenerateService.DidNotReceive().RemoveSelectionWarning(Arg.Any<string>(), Arg.Any<List<ReasonModel>>());
        await betBuilderGenerateService.DidNotReceive().WriteLogReason(Arg.Any<BetBuilderRequest>(), Arg.Any<List<ReasonModel>>());
    }

    [Fact]
    public async Task Settle_ValidatePassedAndSelectedMarketEmpty_CallRightSomeMethodAndNotCallExecute()
    {
        var results = new List<Result>();
        var reason = new List<ReasonModel>
        {
            new()
            {
                SelectionType = 1
            }
        };
        betBuilderGenerateService.ValidationBeforeSettle(Arg.Any<BetBuilderRequest>()).Returns((results, reason));
        betBuilderGenerateService.GetInfoMarket(Arg.Any<BetBuilderRequest>()).Returns(string.Empty);
        var result = await service.Settle(new BetBuilderRequest());

        Received.InOrder(() =>
        {
            betBuilderGenerateService.RemoveSelectionWarning(Arg.Any<string>(), Arg.Any<List<ReasonModel>>());
            betBuilderGenerateService.WriteLogReason(Arg.Any<BetBuilderRequest>(), Arg.Any<List<ReasonModel>>());
            betBuilderGenerateService.UpdateMatchInfo(Arg.Any<BetBuilderRequest>(), Arg.Any<string>());
            betBuilderGenerateService.WriteLog(Arg.Any<BetBuilderRequest>());
        });

        Assert.Equal(0, result.ErrorCode);
        await calculateService.DidNotReceive().Execute(Arg.Any<BetBuilderCriteria>());
    }

    //[Fact]
    //public async Task Settle_ValidatePassedAndSelectedMarket_CallRightSomeMethod()
    //{
    //    var results = new List<Result>();
    //    var reason = new List<ReasonModel>
    //    {
    //        new()
    //        {
    //            SelectionType = 1
    //        }
    //    };

    //    betBuilderGenerateService.ValidationBeforeSettle(Arg.Any<BetBuilderRequest>()).Returns((results, reason));
    //    betBuilderGenerateService.GetInfoMarket(Arg.Any<BetBuilderRequest>()).Returns("MarketId");
    //    calculateService.Execute(Arg.Any<BetBuilderCriteria>(), Arg.Any<SiteDbPendingCommand>()).Returns(new Result());
    //    var result = await service.Settle(new BetBuilderRequest());

    //    Received.InOrder(() =>
    //    {
    //        betBuilderGenerateService.RemoveSelectionWarning(Arg.Any<string>(), Arg.Any<List<ReasonModel>>());
    //        betBuilderGenerateService.WriteLogReason(Arg.Any<BetBuilderRequest>(), Arg.Any<List<ReasonModel>>());
    //        betBuilderGenerateService.UpdateMatchInfo(Arg.Any<BetBuilderRequest>(), Arg.Any<string>());
    //        betBuilderGenerateService.WriteLog(Arg.Any<BetBuilderRequest>());
    //        //calculateService.Execute(Arg.Any<BetBuilderCriteria>(), Arg.Any<SiteDbPendingCommand>());
    //    });

    //    Assert.Equal(0, result.ErrorCode);
    //}
}