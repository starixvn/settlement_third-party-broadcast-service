using Fanex.Settlement.MessageBridge.ResultAmendment;
using MassTransit;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;
using ThirdPartyBroadcast.Consumer.Models.BetBuilder;
using ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;
using ResourceItem = Fanex.Settlement.MessageBridge.ResultAmendment.ResourceItem;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.BetBuilder;

public class BetBuilderResultAmendmentServiceTests
{
    private readonly MatchService matchService;
    private readonly XmlConvertService xmlConvertService;
    private readonly IPublishEndpoint publishEndpoint;
    private readonly IDynamicRepository repository;
    private readonly BetBuilderResultAmendmentService service;

    public BetBuilderResultAmendmentServiceTests()
    {
        matchService = Substitute.For<MatchService>();
        xmlConvertService = Substitute.For<XmlConvertService>();
        publishEndpoint = Substitute.For<IPublishEndpoint>();
        repository = Substitute.For<IDynamicRepository>();

        service = new BetBuilderResultAmendmentService(
            matchService,
            xmlConvertService,
            publishEndpoint,
            repository);
    }

    [Fact]
    public async Task PushResultAmendment_NoResultsChanged_ReturnsFalse()
    {
        // Arrange
        var request = new BetBuilderRequest { MatchId = 1, Action = "action" };
        var resourceItems = new List<ResourceItem>();
        var criteria = new BetBuilderCriteria();

        xmlConvertService.Serialize(Arg.Any<object>()).Returns("<xml/>");
        matchService.GetMatchInfo(Arg.Any<int>())
            .Returns(Task.FromResult(new MatchInfo { SportType = 1 }));
        matchService.GetResultChanged(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ResultChanged>()));

        // Act
        var result = await service.PushResultAmendment(request, resourceItems, criteria);

        // Assert
        Assert.False(result);
        await publishEndpoint.DidNotReceive().Publish(Arg.Any<ResultAmended>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushUpdateStatusResult_PublishesResultAmendSettled()
    {
        // Arrange
        int matchId = 1;
        string broadcastSessionId = "session";
        bool isOrg = true;

        // Act
        await service.PushUpdateStatusResult(matchId, broadcastSessionId, isOrg);

        // Assert
        await publishEndpoint.Received(1).Publish(Arg.Any<ResultAmendSettled>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushResultAmendment_WhenResultChanged_EnrichesMarkets_AndPublishes_ReturnsTrue()
    {
        // Arrange
        var request = new BetBuilderRequest { MatchId = 20, Action = "SETTLE" };
        var resources = new List<ResourceItem>
            {
                new ResourceItem { ResourceId = "100", HomeValue = "Win" }
            };
        const string selectionType = "S1";

        xmlConvertService.Serialize(Arg.Any<object>()).Returns("<xml/>");

        matchService.GetMatchInfo(request.MatchId)
            .Returns(Task.FromResult(new MatchInfo { SportType = 7 }));

        matchService.GetResultChanged(request.MatchId, request.Action, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ResultChanged>>(new[]
            {
                    new ResultChanged { ResourceId = selectionType }
            }));

        repository.FetchAsync<SelectionMarket>(Arg.Any<GetSelectionMarketsCriteria>())
            .Returns(Task.FromResult<IEnumerable<SelectionMarket>>(new[]
            {
                    new SelectionMarket { SelectionType = selectionType, MarketId = "MKT-1" }
            }));

        var criteria = new BetBuilderCriteria { };

        // Act
        var ok = await service.PushResultAmendment(request, resources, criteria);

        // Assert
        Assert.True(ok);
        await publishEndpoint.Received(1).Publish(Arg.Any<ResultAmended>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task PushResultAmendment_WhenNoResultChanged_ReturnsFalse_AndDoesNotPublish()
    {
        // Arrange
        var request = new BetBuilderRequest { MatchId = 10, Action = "SETTLE" };
        var resources = new List<ResourceItem>
            {
                // Ensure these IDs align with ResourceId usage in service
                new ResourceItem {ResourceId = "1", HomeValue = "Win"}
            };
        var criteria = new BetBuilderCriteria { };

        xmlConvertService.Serialize(Arg.Any<object>()).Returns("<xml/>");
        matchService.GetMatchInfo(Arg.Any<int>())
            .Returns(Task.FromResult(new MatchInfo { SportType = 1 }));
        matchService.GetResultChanged(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult(Enumerable.Empty<ResultChanged>()));

        // Act
        var ok = await service.PushResultAmendment(request, resources, criteria);

        // Assert
        Assert.False(ok);
        await publishEndpoint.DidNotReceive().Publish(Arg.Any<ResultAmended>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task PushResultAmendment_DeduplicatesSelectionTypes_WhenFetchingMarkets()
    {
        // Arrange
        var request = new BetBuilderRequest { MatchId = 4, Action = "SETTLE" };
        var resources = new List<ResourceItem>(); // keep empty to avoid relying on ResourceId presence
        var criteria = new BetBuilderCriteria { };

        xmlConvertService.Serialize(Arg.Any<object>()).Returns("<xml/>");
        matchService.GetMatchInfo(request.MatchId)
            .Returns(Task.FromResult(new MatchInfo { SportType = 2 }));
        matchService.GetResultChanged(request.MatchId, request.Action, Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<ResultChanged>>(new[]
            {
                    new ResultChanged { ResourceId = "S1" },
                    new ResultChanged { ResourceId = "S2" },
                    new ResultChanged { ResourceId = "S1" }, // duplicate
            }));

        repository.FetchAsync<SelectionMarket>(Arg.Any<GetSelectionMarketsCriteria>())
            .Returns(Task.FromResult<IEnumerable<SelectionMarket>>(new[]
            {
                    new SelectionMarket { SelectionType = "S1", MarketId = "M1" },
                    new SelectionMarket { SelectionType = "S2", MarketId = "M2" },
            }));

        // Act
        var ok = await service.PushResultAmendment(request, resources, criteria);

        // Assert
        Assert.True(ok);

        await repository.Received(1)
            .FetchAsync<SelectionMarket>(Arg.Is<GetSelectionMarketsCriteria>(c =>
                // Order is not guaranteed; assert set equality
                c.MatchId == request.MatchId &&
                c.SelectionTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .OrderBy(x => x)
                    .SequenceEqual(new[] { "S1", "S2" })));
    }
}