using System;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Constants;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services;

public class TicketInfoServiceTests
{
    private readonly IOptions<AppSettingsKey> appSettingsKey;
    private readonly IDynamicRepository repository;
    private readonly TicketInfoService ticketInfoService;

    public TicketInfoServiceTests()
    {
        appSettingsKey = Substitute.For<IOptions<AppSettingsKey>>();
        repository = Substitute.For<IDynamicRepository>();
        ticketInfoService = new TicketInfoServiceImp(appSettingsKey, repository);
    }

    [Fact]
    public async Task GetTicketsByAction_ShouldReturnEmptyList_WhenActionIsNotRefund()
    {
        // Arrange
        int matchId = 1;
        short sportType = 1;
        string action = "CAL";

        // Act
        var result = await ticketInfoService.GetTicketsByAction(matchId, sportType, action);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTicketsByAction_ShouldReturnTickets_WhenActionIsRefund()
    {
        // Arrange
        int matchId = 1;
        short sportType = 1;
        string action = ActionName.Refund;
        var tickets = new List<TicketSettleInfo> { new TicketSettleInfo() };
        repository.FetchAsync<TicketSettleInfo>(Arg.Any<TicketSettleCriteria>()).Returns(tickets);
        appSettingsKey.Value.Returns(new AppSettingsKey { SiteDbServer = new List<string> { "Server1" } });

        // Act
        var result = await ticketInfoService.GetTicketsByAction(matchId, sportType, action);

        // Assert
        Assert.Single(result);
        Assert.Equal(tickets, result);
    }

    [Fact]
    public async Task GetTicketsByTransIds_ShouldReturnEmptyList_WhenNoTicketsProvided()
    {
        // Arrange
        int matchId = 1;
        short sportType = 1;
        var tickets = new List<TicketSettleInfo>();

        // Act
        var result = await ticketInfoService.GetTicketsByTransIds(matchId, sportType, tickets);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTicketsByTransIds_ShouldReturnTickets_WhenTicketsProvided()
    {
        // Arrange
        int matchId = 1;
        short sportType = 1;
        var tickets = new List<TicketSettleInfo> { new TicketSettleInfo { TransId = 123 } };
        var expectedTickets = new List<TicketSettleInfo> { new TicketSettleInfo() };
        repository.FetchAsync<TicketSettleInfo>(Arg.Any<TicketSettleCriteria>()).Returns(expectedTickets);
        appSettingsKey.Value.Returns(new AppSettingsKey { SiteDbServer = new List<string> { "Server1" } });

        // Act
        var result = await ticketInfoService.GetTicketsByTransIds(matchId, sportType, tickets);

        // Assert
        Assert.Equal(expectedTickets, result);
    }

    [Fact]
    public async Task GetTicketsAllSites_ShouldReturnCombinedTicketsFromAllSites()
    {
        // Arrange
        int matchId = 1;
        short sportType = 1;
        string action = ActionName.Refund;
        var ticketsFromSite1 = new List<TicketSettleInfo> { new TicketSettleInfo { TransId = 123 } };
        var ticketsFromSite2 = new List<TicketSettleInfo> { new TicketSettleInfo { TransId = 456 } };
        repository.FetchAsync<TicketSettleInfo>(Arg.Is<TicketSettleCriteria>(c => c.DbServer == "Server1")).Returns(ticketsFromSite1);
        repository.FetchAsync<TicketSettleInfo>(Arg.Is<TicketSettleCriteria>(c => c.DbServer == "Server2")).Returns(ticketsFromSite2);
        appSettingsKey.Value.Returns(new AppSettingsKey { SiteDbServer = new List<string> { "Server1", "Server2" } });

        // Act
        var result = await ticketInfoService.GetTicketsByAction(matchId, sportType, action);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.TransId == 123);
        Assert.Contains(result, t => t.TransId == 456);
    }
}