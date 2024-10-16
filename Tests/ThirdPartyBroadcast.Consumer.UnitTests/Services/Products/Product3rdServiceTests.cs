using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class Product3rdServiceTests
{
    private readonly IDynamicRepository repository;
    private readonly LogDirectlyService logDirectlyService;
    private readonly CalculateService calculateService;
    private readonly TicketInfoService ticketService;
    private readonly Product3rdServiceImp service;

    public Product3rdServiceTests()
    {
        repository = Substitute.For<IDynamicRepository>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        calculateService = Substitute.For<CalculateService>();
        ticketService = Substitute.For<TicketInfoService>();
        var announcementService = Substitute.For<AnnouncementService>();
        var refDataService = Substitute.For<RefDataService>();
        var recipientMessageService = Substitute.For<RecipientMessageService>();

        service = new Product3rdServiceImp(
            repository,
            logDirectlyService,
            calculateService,
            ticketService,
            announcementService,
            refDataService,
            recipientMessageService
        );
    }

    [Fact]
    public async Task Settle_ShouldReturnErrorRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = new VirtualSportFiveRequest
        {
            MatchId = 1,
            Action = "InvalidAction",
            SelectedResources = "<Resources></Resources>"
        };
        var matchInfo = new MatchInfo { MatchId = 1, SportType = 147, EventDate = DateTime.Now };
        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(Result.ErrorRequest().ErrorCode, result.ErrorCode);
        Assert.Equal(Result.ErrorRequest().ErrorMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task Settle_ShouldLogMessage_WhenCAL()
    {
        // Arrange
        var request = new VirtualSportFiveRequest
        {
            MatchId = 1,
            Action = "CAL",
            SelectedResources = "<Resources><Resource Id=\"01\" Home=\"1\" Away=\"0\" /><Resource Id=\"00\" Home=\"1\" Away=\"3\" /></Resources>"
        };

        var matchInfo = new MatchInfo { MatchId = 1, SportType = 147, EventDate = DateTime.Now };

        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        ticketService.GetTicketsByAction(matchInfo.MatchId, matchInfo.SportType, request.Action).Returns(new List<TicketSettleInfo>());
        calculateService.Execute(Arg.Any<VirtualSportFiveSettleCriteria>()).Returns(new Result());

        // Act
        var result = await service.Settle(request);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Any<Match3rdUpdateCriteria>());
        await logDirectlyService.Received(1).WriteLog(Arg.Any<int>(), Arg.Any<string>());
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task Settle_ActionIsRefundAndPushFinalResult_CallRightMethodAndSetCorrectValue()
    {
        // Arrange
        var request = new VirtualSportFiveRequest
        {
            MatchId = 1,
            Action = "REFUND",
            SelectedResources = "<Resources><Resource Id=\"00\" Home=\"\" Away=\"\" /></Resources>"
        };

        var matchInfo = new MatchInfo { MatchId = 1, SportType = 148, EventDate = DateTime.Now };

        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        ticketService.GetTicketsByAction(matchInfo.MatchId, matchInfo.SportType, request.Action).Returns([]);
        calculateService.Execute(Arg.Any<VirtualSportFiveSettleCriteria>()).Returns(new Result());

        // Act
        var result = await service.Settle(request);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Is<Match3rdUpdateCriteria>(x => x.BetStatus == "Refund FT"));
        await logDirectlyService.Received(1).WriteLog(Arg.Any<int>(), Arg.Any<string>());
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task Settle_ActionIsRefundPushHalfAndFinalResult_CallRightMethodAndSetCorrectValue()
    {
        // Arrange
        var request = new VirtualSportFiveRequest
        {
            MatchId = 1,
            Action = "REFUND",
            SelectedResources = "<Resources><Resource Id=\"00\" Home=\"\" Away=\"\" /><Resource Id=\"01\" Home=\"\" Away=\"\" /></Resources>"
        };

        var matchInfo = new MatchInfo { MatchId = 1, SportType = 148, EventDate = DateTime.Now };

        repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        ticketService.GetTicketsByAction(matchInfo.MatchId, matchInfo.SportType, request.Action).Returns([]);
        calculateService.Execute(Arg.Any<VirtualSportFiveSettleCriteria>()).Returns(new Result());

        // Act
        var result = await service.Settle(request);

        // Assert
        await repository.Received(1).ExecuteAsync(Arg.Is<Match3rdUpdateCriteria>(x => x.BetStatus == "Refund"));
        await logDirectlyService.Received(1).WriteLog(Arg.Any<int>(), Arg.Any<string>());
        Assert.True(result.IsSuccessful);
    }
}