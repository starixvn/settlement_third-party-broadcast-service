using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualSportFive;
using ThirdPartyBroadcast.Consumer.Models;
using ThirdPartyBroadcast.Consumer.Services;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class Product3rdServiceTests
{
    private readonly IDynamicRepository _repository;
    private readonly LogDirectlyService _logDirectlyService;
    private readonly CalculateService _calculateService;
    private readonly TicketInfoService _ticketService;
    private readonly AnnouncementService _announcementService;
    private readonly RefDataService _refDataService;
    private readonly RecipientMessageService _recipientMessageService;
    private readonly Product3rdServiceImp _service;

    public Product3rdServiceTests()
    {
        _repository = Substitute.For<IDynamicRepository>();
        _logDirectlyService = Substitute.For<LogDirectlyService>();
        _calculateService = Substitute.For<CalculateService>();
        _ticketService = Substitute.For<TicketInfoService>();
        _announcementService = Substitute.For<AnnouncementService>();
        _refDataService = Substitute.For<RefDataService>();
        _recipientMessageService = Substitute.For<RecipientMessageService>();

        _service = new Product3rdServiceImp(
            _repository,
            _logDirectlyService,
            _calculateService,
            _ticketService,
            _announcementService,
            _refDataService,
            _recipientMessageService
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
        _repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        // Act
        var result = await _service.Settle(request);

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

        _repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        _ticketService.GetTicketsByAction(matchInfo.MatchId, matchInfo.SportType, request.Action).Returns(new List<TicketSettleInfo>());
        _calculateService.Execute(Arg.Any<VirtualSportFiveSettleCriteria>()).Returns(new Result());

        // Act
        var result = await _service.Settle(request);

        // Assert
        await _repository.Received(1).ExecuteAsync(Arg.Any<Match3rdUpdateCriteria>());
        await _logDirectlyService.Received(1).WriteLog(Arg.Any<int>(), Arg.Any<string>());
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public async Task Settle_ShouldLogMessage_WhenRefund()
    {
        // Arrange
        var request = new VirtualSportFiveRequest
        {
            MatchId = 1,
            Action = "REFUND",
            SelectedResources = "<Resources><Resource Id=\"00\" Home=\"\" Away=\"\" /></Resources>"
        };

        var matchInfo = new MatchInfo { MatchId = 1, SportType = 148, EventDate = DateTime.Now };

        _repository.GetAsync<MatchInfo>(Arg.Any<Match3rdSelCriteria>()).Returns(matchInfo);

        _ticketService.GetTicketsByAction(matchInfo.MatchId, matchInfo.SportType, request.Action).Returns(new List<TicketSettleInfo>());
        _calculateService.Execute(Arg.Any<VirtualSportFiveSettleCriteria>()).Returns(new Result());

        // Act
        var result = await _service.Settle(request);

        // Assert
        await _repository.Received(1).ExecuteAsync(Arg.Is<Match3rdUpdateCriteria>(x => x.BetStatus == "Refund"));
        await _logDirectlyService.Received(1).WriteLog(Arg.Any<int>(), Arg.Any<string>());
        Assert.True(result.IsSuccessful);
    }
}