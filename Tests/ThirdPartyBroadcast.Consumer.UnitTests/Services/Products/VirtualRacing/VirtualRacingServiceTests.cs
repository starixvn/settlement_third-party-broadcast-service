using Settlement.Business.Core.Models;
using Settlement.Business.Services;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;
using Result = ThirdPartyBroadcast.Consumer.Models.Result;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.VirtualRacing;

public class VirtualRacingServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly MatchService broadcastMatchService;
    private readonly AnnouncementService announcementService;
    private readonly RecipientMessageService recipientMessageService;
    private readonly TicketInfoService ticketService;
    private readonly VirtualRacingDependencies dependencies;
    private readonly VirtualRacingServiceImp service;

    public VirtualRacingServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        broadcastMatchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();
        recipientMessageService = Substitute.For<RecipientMessageService>();
        ticketService = Substitute.For<TicketInfoService>();

        var racersValidatorService = Substitute.For<RacersValidatorService>();
        var settleSitesService = Substitute.For<SettleSitesService>();
        var matchService = Substitute.For<Settlement.Business.Data.Services.MatchService>();
        var outrightResultService = Substitute.For<OutrightResultService>();

        dependencies = new VirtualRacingDependencies(
            racersValidatorService,
            settleSitesService,
            matchService,
            outrightResultService);

        service = new VirtualRacingServiceImp(
            repository,
            broadcastMatchService,
            announcementService,
            recipientMessageService,
            ticketService,
            dependencies);
    }

    [Fact]
    public async Task Settle_ValidationFails_ReturnsValidationResult()
    {
        // Arrange
        var raceInfo = CreateRaceInfo();
        var request = CreateValidRequest();
        var validationResult = new Result { ErrorCode = 1, ErrorMessage = "Validation failed" };
        dependencies.MatchService.GetRacingInfo(Arg.Any<int>()).Returns(raceInfo);
        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(validationResult);

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(validationResult.ErrorCode, result.ErrorCode);
        Assert.Equal(validationResult.ErrorMessage, result.ErrorMessage);
        await ticketService.DidNotReceive().GetTicketsByAction(Arg.Any<int>(), Arg.Any<short>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Settle_ValidationPasses_ExecutesFullSettleFlow()
    {
        // Arrange
        var request = CreateValidRequest();
        var raceInfo = CreateRaceInfo();
        var tickets = new List<TicketSettleInfo>();
        var settleResult = new Result { ErrorCode = 0 };

        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(new Result());
        dependencies.MatchService.GetRacingInfo(request.EventId).Returns(raceInfo);
        ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action).Returns(tickets);
        dependencies.SettleSitesService.Settle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>(), Arg.Any<IEnumerable<Resources>>())
            .Returns(settleResult);

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(settleResult.ErrorCode, result.ErrorCode);
        await dependencies.MatchService.Received(1).GetRacingInfo(request.EventId);
        await ticketService.Received(1).GetTicketsByAction(request.EventId, request.SportType, request.Action);
        await broadcastMatchService.Received(1).UpdateMatchInfoRacing(request);
        await broadcastMatchService.Received(1).WriteLog(request.EventId);
        await dependencies.SettleSitesService.Received(1).Settle(request, raceInfo.Outright, raceInfo.Resources);
    }

    [Fact]
    public async Task Settle_CalculateAction_LogsWithResults()
    {
        // Arrange
        var request = CreateValidRequest();
        var raceInfo = CreateRaceInfo();
        var tickets = new List<TicketSettleInfo>();
        var settleResult = new Result { ErrorCode = 0 };
        var placingResults = new List<Outright>
        {
            new() { Placing = 1, RacerId = 1 },
            new() { Placing = 2, RacerId = 2 }
        };

        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(new Result());
        dependencies.MatchService.GetRacingInfo(request.EventId).Returns(raceInfo);
        ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action).Returns(tickets);
        dependencies.SettleSitesService.Settle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>(), Arg.Any<IEnumerable<Resources>>())
            .Returns(settleResult);
        dependencies.OutrightResultService.CorrectOutrightResult(request.SportType, request.Racers, Arg.Any<IEnumerable<Outright>>())
            .Returns(placingResults);

        // Act
        await service.Settle(request);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Is<WriteLogCriteria>(c =>
            c.Remark.Contains("Results: 1. 1; 2. 2")));
    }

    [Fact]
    public async Task Settle_RefundAction_LogsWithoutResults()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Action = ActionName.Refund;
        var raceInfo = CreateRaceInfo();
        var tickets = new List<TicketSettleInfo>();
        var settleResult = new Result { ErrorCode = 0 };

        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(new Result());
        dependencies.MatchService.GetRacingInfo(request.EventId).Returns(raceInfo);
        ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action).Returns(tickets);
        dependencies.SettleSitesService.Settle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>(), Arg.Any<IEnumerable<Resources>>())
            .Returns(settleResult);

        // Act
        await service.Settle(request);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Is<WriteLogCriteria>(c =>
            c.Remark.Contains("IsAuto: 1") && !c.Remark.Contains("Results:")));
        dependencies.OutrightResultService.DidNotReceive().CorrectOutrightResult(Arg.Any<int>(), Arg.Any<byte>(), Arg.Any<IEnumerable<Outright>>());
    }

    [Fact]
    public async Task Settle_SettleError_LogsErrorMessage()
    {
        // Arrange
        var request = CreateValidRequest();
        var raceInfo = CreateRaceInfo();
        var tickets = new List<TicketSettleInfo>();
        var settleResult = new Result { ErrorCode = 1, ErrorMessage = "Settle failed" };

        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(new Result());
        dependencies.MatchService.GetRacingInfo(request.EventId).Returns(raceInfo);
        ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action).Returns(tickets);
        dependencies.SettleSitesService.Settle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>(), Arg.Any<IEnumerable<Resources>>())
            .Returns(settleResult);

        // Act
        await service.Settle(request);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Is<WriteLogCriteria>(c =>
            c.Remark.Contains("<br/>Settle failed")));
    }

    [Fact]
    public async Task Settle_SuccessfulSettle_SendsMessagesAndAnnouncements()
    {
        // Arrange
        var request = CreateValidRequest();
        var raceInfo = CreateRaceInfo();
        var tickets = new List<TicketSettleInfo>();
        var settleResult = new Result { ErrorCode = 0 };
        var matchInfo = new MatchInfo { MatchId = request.EventId };

        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(new Result());
        dependencies.MatchService.GetRacingInfo(request.EventId).Returns(raceInfo);
        ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action).Returns(tickets);
        dependencies.SettleSitesService.Settle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>(), Arg.Any<IEnumerable<Resources>>())
            .Returns(settleResult);
        broadcastMatchService.GetRacingInfo(raceInfo.RaceInfo).Returns(matchInfo);

        // Act
        await service.Settle(request);

        // Assert
        await recipientMessageService.Received(1).Message(request.Action, matchInfo);
        await announcementService.Received(1).SendAnnounceRefund(tickets, matchInfo, request.AdminId);
    }

    [Fact]
    public async Task Settle_UpdatesRequestProperties()
    {
        // Arrange
        var request = CreateValidRequest();
        var raceInfo = CreateRaceInfo();
        var tickets = new List<TicketSettleInfo>();
        var settleResult = new Result { ErrorCode = 0 };

        dependencies.RacersValidatorService.ValidateBeforeSettle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>())
            .Returns(new Result());
        dependencies.MatchService.GetRacingInfo(request.EventId).Returns(raceInfo);
        ticketService.GetTicketsByAction(request.EventId, request.SportType, request.Action).Returns(tickets);
        dependencies.SettleSitesService.Settle(Arg.Any<VirtualRacingRequest>(), Arg.Any<IEnumerable<Outright>>(), Arg.Any<IEnumerable<Resources>>())
            .Returns(settleResult);

        // Act
        await service.Settle(request);

        // Assert
        Assert.Equal(raceInfo.RaceInfo.SportType, request.SportType);
        Assert.Equal(raceInfo.RaceInfo.Racers, request.Racers);
    }

    private static VirtualRacingRequest CreateValidRequest()
    {
        return new VirtualRacingRequest
        {
            EventId = 123,
            Action = ActionName.Calculate,
            SportType = 1,
            Racers = 6,
            AdminId = 456,
            Ip = "127.0.0.1"
        };
    }

    private static (RaceInfo RaceInfo, IEnumerable<Outright> Outright, IEnumerable<Resources> Resources) CreateRaceInfo()
    {
        var raceInfo = new RaceInfo
        {
            EventId = 123,
            SportType = 1,
            Racers = 6,
            EventStatus = "completed"
        };

        var outright = new List<Outright>
        {
            new() { Orid = 1, Placing = 1, RacerId = 1 },
            new() { Orid = 2, Placing = 2, RacerId = 2 },
            new() { Orid = 3, Placing = 3, RacerId = 3 },
            new() { Orid = 4, Placing = 4, RacerId = 4 },
            new() { Orid = 5, Placing = 5, RacerId = 5 },
            new() { Orid = 6, Placing = 6, RacerId = 6 }
        };

        var resources = new List<Resources>();

        return (raceInfo, outright, resources);
    }
}