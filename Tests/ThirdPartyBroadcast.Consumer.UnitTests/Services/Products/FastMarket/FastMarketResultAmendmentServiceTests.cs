using Fanex.Settlement.MessageBridge.Constants;
using Fanex.Settlement.MessageBridge.ResultAmendment;
using Fanex.Settlement.ScaleOut.Constants;
using MassTransit;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Models.FM;
using ThirdPartyBroadcast.Consumer.Services.Products.FastMarket;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.FastMarket;

public class FastMarketResultAmendmentServiceTests
{
    private readonly LogDirectlyService _logDirectlyService;
    private readonly MatchService _matchService;
    private readonly XmlConvertService _xmlConvertService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly FastMarketResultAmendmentServiceImp _service;

    public FastMarketResultAmendmentServiceTests()
    {
        _logDirectlyService = Substitute.For<LogDirectlyService>();
        _matchService = Substitute.For<MatchService>();
        _xmlConvertService = Substitute.For<XmlConvertService>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        _service = new FastMarketResultAmendmentServiceImp(
            _logDirectlyService,
            _matchService,
            _xmlConvertService,
            _publishEndpoint
        );
    }

    [Fact]
    public async Task PushResultAmendment_ShouldLogAndPublishResultAmendment()
    {
        // Arrange
        var request = new FastMarketRequest
        {
            MatchId = 123,
            Action = "UPDATE",
            Results = "<BetTypeXML><BetType><Id>1</Id><Items><Position>1</Position></Items></BetType></BetTypeXML>"
        };

        var criteria = new FastMarketCriteria
        {
            Action = "UPDATE",
            AdminId = 456,
            MatchId = 123
        };

        var resultChanged = new List<ResultChanged>
        {
            new() { ResourceId = "1", OtherInfo = "1", HomeValue = "2" }
        };

        _xmlConvertService.Deserialize<BetTypeXML>(request.Results)
            .Returns(new BetTypeXML
            {
                BetType = [new BetTypeElement { Id = 1, Items = [new ItemElement { Position = 1 }] }]
            });

        _matchService.GetFastMarketResult(request.MatchId)
            .Returns(new List<FastMarketResult>
            {
                new() { BetType = 1, Position = 1, Result = 2 }
            });

        _xmlConvertService.Serialize(Arg.Any<Resources>())
            .Returns("<Resources><ResourceItems><ResourceItem><ResourceId>1</ResourceId><OtherInfo>1</OtherInfo><HomeValue>2</HomeValue></ResourceItem></ResourceItems></Resources>");

        _matchService.GetResultChanged(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(resultChanged);

        // Act
        await _service.PushResultAmendment(request, criteria);

        // Assert
        await _logDirectlyService.Received(1).WriteLog(Arg.Is<InsertLogCriteria>(c =>
            c.Action == criteria.Action &&
            c.AdminId == criteria.AdminId &&
            c.MatchId == criteria.MatchId &&
            c.Mode == Mode.FastMarket));

        await _publishEndpoint.Received(1).Publish(Arg.Is<ResultAmended>(r =>
            r.MatchId == request.MatchId &&
            r.BroadcastSessionId == criteria.BroadcastSessionId &&
            r.Action == request.Action &&
            r.Type == ResultAmendmentType.FastMarket), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PushResultAmendment_WithTimeMachineHdp_ShouldCalculateCorrectValues()
    {
        // Arrange
        var request = new FastMarketRequest
        {
            MatchId = 123,
            Action = "UPDATE",
            Results = "<BetTypeXML><BetType><Id>229</Id><Items><Position>1</Position></Items></BetType></BetTypeXML>"
        };

        var criteria = new FastMarketCriteria
        {
            Action = "UPDATE",
            AdminId = 456,
            MatchId = 123
        };

        _xmlConvertService.Deserialize<BetTypeXML>(request.Results)
            .Returns(new BetTypeXML
            {
                BetType = [new BetTypeElement { Id = 229, Items = [new ItemElement { Position = 1 }] }]
            });

        _matchService.GetFastMarketResult(request.MatchId)
            .Returns(new List<FastMarketResult>
            {
                new() { BetType = 229, Position = 1, Result = 15 }
            });

        _xmlConvertService.Serialize(Arg.Any<Resources>())
            .Returns("<Resources><ResourceItems><ResourceItem><ResourceId>229</ResourceId><OtherInfo>1</OtherInfo><HomeValue>5</HomeValue></ResourceItem></ResourceItems></Resources>");

        _matchService.GetResultChanged(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new List<ResultChanged>());

        // Act
        await _service.PushResultAmendment(request, criteria);

        // Assert
        await _logDirectlyService.Received(1).WriteLog(Arg.Is<InsertLogCriteria>(c =>
            c.Action == criteria.Action &&
            c.AdminId == criteria.AdminId &&
            c.MatchId == criteria.MatchId &&
            c.Mode == Mode.FastMarket));

        await _publishEndpoint.Received(0).Publish(Arg.Is<ResultAmended>(r =>
            r.MatchId == request.MatchId &&
            r.BroadcastSessionId == criteria.BroadcastSessionId &&
            r.Action == request.Action &&
            r.Type == ResultAmendmentType.FastMarket), Arg.Any<CancellationToken>());
    }
}