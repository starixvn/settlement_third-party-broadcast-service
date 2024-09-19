using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;
using ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.BetBuilder;

public class BetBuilderGenerateServiceTests
{
    private readonly ScaleOutSettleService scaleOutSettleService;

    private readonly IDynamicRepository repository;

    private readonly BetBuilderGenerateServiceImp service;

    private readonly LogDirectlyService logDirectlyService;

    public BetBuilderGenerateServiceTests()
    {
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        repository = Substitute.For<IDynamicRepository>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        service = new BetBuilderGenerateServiceImp(scaleOutSettleService, repository, logDirectlyService, new XmlConvertServiceImp());
    }

    [Fact]
    public async Task WriteLogReason_ExistedReason_ReturnsWriteLog()
    {
        var request = new BetBuilderRequest
        {
            AdminId = 1,
            MatchId = 1,
            Resources = "<Resources><Resource Id=\"200\" Result=\"Won\" /><Resource Id=\"201\" Result=\"Won\" /><Resource Id=\"202\" Result=\"Lose\" /></Resources>"
        };
        var reason = new List<ReasonModel>
        {
            new()
            {
                SelectionType = 201,
                Reason = "Settled"
            }
        };

        await service.WriteLogReason(request, reason);

        const string expectedRemark = "Failed Auto Payout: Selection: 201 Reason: Settled.<br/>";
        await repository.Received().ExecuteAsync(Arg.Is<WriteLogCriteria>(c => c.Remark.Equals(expectedRemark)));
    }

    [Fact]
    public void RemoveSelectionWarning_NotExistedReason_ReturnsRightSelection()
    {
        const string resources = "<Resources><Resource Id=\"200\" Result=\"Won\" /><Resource Id=\"201\" Result=\"Won\" /><Resource Id=\"202\" Result=\"Lose\" /></Resources>";

        var result = service.RemoveSelectionWarning(resources, new List<ReasonModel>());

        const string expectedSelection = "<Resources><Resource Id=\"200\" Result=\"Won\" /><Resource Id=\"201\" Result=\"Won\" /><Resource Id=\"202\" Result=\"Lose\" /></Resources>";

        Assert.Equal(expectedSelection, result);
    }

    [Fact]
    public void RemoveSelectionWarning_IsExistedReason_ReturnsRightSelection()
    {
        const string resources = "<Resources><Resource Id=\"200\" Result=\"Won\" /><Resource Id=\"201\" Result=\"Won\" /><Resource Id=\"202\" Result=\"Lose\" /></Resources>";
        var reason = new List<ReasonModel>
        {
            new()
            {
                SelectionType = 201,
                Reason = "Settled"
            }
        };

        var result = service.RemoveSelectionWarning(resources, reason);

        const string expectedSelection = "<Resources><Resource Id=\"200\" Result=\"Won\" /><Resource Id=\"202\" Result=\"Lose\" /></Resources>";

        Assert.Equal(expectedSelection, result);
    }

    [Fact]
    public void RemoveSelectionWarning_AllSelectionIsExistedReasonWarning_ReturnsEmpty()
    {
        const string resources = "<Resources><Resource Id=\"200\" Result=\"Won\" /><Resource Id=\"201\" Result=\"Won\" /><Resource Id=\"202\" Result=\"Lose\" /></Resources>";
        var reason = new List<ReasonModel>
        {
            new()
            {
                SelectionType = 201,
                Reason = "Settled"
            },
            new()
            {
                SelectionType = 200,
                Reason = "Settled"
            },
            new()
            {
                SelectionType = 202,
                Reason = "Settled"
            }
        };

        var result = service.RemoveSelectionWarning(resources, reason);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInfoMarket_NotExistedMarketIds_ReturnsEmpty()
    {
        repository.FetchAsync<string>(Arg.Any<GetMarketIdCriteria>()).Returns([]);
        var result = await service.GetInfoMarket(new BetBuilderRequest());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetInfoMarket_IsExistedMarketIds_ReturnsExpectedValue()
    {
        repository.FetchAsync<string>(Arg.Any<GetMarketIdCriteria>()).Returns(["200"]);
        var result = await service.GetInfoMarket(new BetBuilderRequest());

        const string expectedValue = "<Resources><rsc m=\"200\" /></Resources>";
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task UpdateMatchInfo_ResourcesEmpty_NotCallUpdateMatchInfoMethod()
    {
        await service.UpdateMatchInfo(new BetBuilderRequest(), string.Empty);

        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
    }

    [Fact]
    public async Task UpdateMatchInfo_ResourcesNotEmpty_CallUpdateMatchInfoMethod()
    {
        var request = new BetBuilderRequest
        {
            Resources = "Resources"
        };
        await service.UpdateMatchInfo(request, string.Empty);

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
    }

    [Fact]
    public async Task WriteLog_ResourcesEmpty_CallWriteLogWithRightMessage()
    {
        var request = new BetBuilderRequest
        {
            AdminId = 1,
            MatchId = 1,
            Action = "CAL",
            IsAuto = true,
            Resources = string.Empty
        };

        await service.WriteLog(request);

        const string expectedMessage = "NAP: Calculate CAL for matchId: 1 IsAuto:True; BetType:38";
        await logDirectlyService.Received().WriteLog(Arg.Any<int>(), Arg.Is<string>(expectedMessage));
    }

    [Fact]
    public async Task WriteLog_ResourcesNotEmpty_CallWriteLogWithRightMessage()
    {
        var request = new BetBuilderRequest
        {
            AdminId = 1,
            MatchId = 1,
            Action = "CAL",
            IsAuto = true,
            Resources = "Resource"
        };

        await service.WriteLog(request);

        const string expectedMessage = "NAP: Calculate CAL for matchId: 1 Resource;<br/> IsAuto:True; BetType:38";
        await logDirectlyService.Received().WriteLog(Arg.Any<int>(), Arg.Is<string>(expectedMessage));
    }
}