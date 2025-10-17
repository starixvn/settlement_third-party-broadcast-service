using Fanex.SiteDbConnectionManager.Client.Models;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;
using ThirdPartyBroadcast.Consumer.Models.BetBuilder;
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

        //    mapper.Map<BetBuilderCriteria>(Arg.Any<Requests.BetBuilderRequest>())
        //.Returns(callInfo =>
        //{
        //    var reqArg = callInfo.Arg<Requests.BetBuilderRequest>();
        //    Assert.Equal("<filtered/>", reqArg.Resources);
        //    return mappedCriteria;
        //});

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


    [Fact]
    public async Task Settle_WhenReasonsExist_RemovesSelectionAndLogsReason_BeforeProceeding()
    {
        // Arrange
        var request = new Requests.BetBuilderRequest
        {
            MatchId = 5,
            Action = "SETTLE",
            AdminId = 50,
            Ip = "5.5.5.5",
            Resources = "<resources/>",
        };

        var reasons = new[] { new ReasonModel { SelectionType = 1, Reason = "Blocked" } };

        betBuilderGenerateService.ValidationBeforeSettle(request)
            .Returns((new[] { new Result() }.AsEnumerable(), reasons));

        betBuilderGenerateService.RemoveSelectionWarning(request.Resources, reasons)
            .Returns("<filtered/>");

        betBuilderGenerateService.GetInfoMarket(Arg.Any<Requests.BetBuilderRequest>())
            .Returns("MKT-3");
        betBuilderGenerateService.UpdateMatchInfo(Arg.Any<Requests.BetBuilderRequest>(), "MKT-3").Returns(Task.CompletedTask);
        betBuilderGenerateService.WriteLog(Arg.Any<Requests.BetBuilderRequest>()).Returns(Task.CompletedTask);

        matchService.GetMatchInfo(request.MatchId)
            .Returns(Task.FromResult(new MatchInfo { SportType = 3 }));

        xmlConvertService.Deserialize<Resources>("<filtered/>")
            .Returns(new Resources { ResourceItem = new List<ThirdPartyBroadcast.Consumer.Models.BetBuilder.ResourceItem>() });
        xmlConvertService.Serialize(Arg.Any<Models.FM.Resources>()).Returns("<sel/>");

        // Capture that mapping sees the filtered Resources
        BetBuilderCriteria mappedCriteria = new() { MatchId = request.MatchId, Action = request.Action };


        betBuilderResultAmendmentService.PushResultAmendment(Arg.Any<Requests.BetBuilderRequest>(),
            Arg.Any<List<Fanex.Settlement.MessageBridge.ResultAmendment.ResourceItem>>(),
            Arg.Any<BetBuilderCriteria>()).Returns(Task.FromResult(false));

        calculateService.Execute(Arg.Any<CalculateCriteria>(), Arg.Any<SiteDbPendingCommand>())
            .Returns(Task.FromResult(new Result { ErrorCode = 0 }));

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(0, result.ErrorCode);

        // Verify reason handling occurred
        betBuilderGenerateService.Received(1).RemoveSelectionWarning("<resources/>", reasons);
        await betBuilderGenerateService.Received(1).WriteLogReason(Arg.Any<Requests.BetBuilderRequest>(), reasons);
    }

    [Fact]
    public async Task Settle_WhenResultAmendFalse_DoesNotPushUpdateStatus()
    {
        // Arrange
        var request = new Requests.BetBuilderRequest
        {
            MatchId = 4,
            Action = "SETTLE",
            AdminId = 40,
            Ip = "4.4.4.4",
            Resources = "<resources/>",
        };

        betBuilderGenerateService.ValidationBeforeSettle(request)
            .Returns((new[] { new Result() }.AsEnumerable(), Enumerable.Empty<ReasonModel>()));

        betBuilderGenerateService.GetInfoMarket(request).Returns("MKT-2");
        betBuilderGenerateService.UpdateMatchInfo(request, "MKT-2").Returns(Task.CompletedTask);
        betBuilderGenerateService.WriteLog(request).Returns(Task.CompletedTask);

        matchService.GetMatchInfo(request.MatchId)
            .Returns(Task.FromResult(new MatchInfo { SportType = 1 }));

        xmlConvertService.Deserialize<Resources>(request.Resources)
            .Returns(new Resources { ResourceItem = new List<ThirdPartyBroadcast.Consumer.Models.BetBuilder.ResourceItem>() });
        xmlConvertService.Serialize(Arg.Any<Models.FM.Resources>()).Returns("<sel/>");

        var mappedCriteria = new BetBuilderCriteria
        {
            MatchId = request.MatchId,
            Action = request.Action
        };
        //mapper.Map<BetBuilderCriteria>(request).Returns(mappedCriteria);

        betBuilderResultAmendmentService.PushResultAmendment(request,
            Arg.Any<List<Fanex.Settlement.MessageBridge.ResultAmendment.ResourceItem>>(),
            Arg.Any<BetBuilderCriteria>()).Returns(Task.FromResult(false));

        calculateService.Execute(Arg.Any<CalculateCriteria>(), Arg.Any<SiteDbPendingCommand>())
            .Returns(Task.FromResult(new Result { ErrorCode = 0 }));

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(0, result.ErrorCode);
        await betBuilderResultAmendmentService.DidNotReceive()
            .PushUpdateStatusResult(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<bool>());
    }
}