using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services;

public class TicketInfoServiceTests
{
    private readonly SiteDbClientService siteDbClientService;

    private readonly IDynamicRepository repository;

    private readonly TicketInfoService ticketInfoService;

    private const int MatchId = 1;

    private const short SportType = 1;

    public TicketInfoServiceTests()
    {
        siteDbClientService = Substitute.For<SiteDbClientService>();
        repository = Substitute.For<IDynamicRepository>();
        ticketInfoService = new TicketInfoServiceImp(repository, siteDbClientService);
    }

    [Fact]
    public async Task GetTicketsByAction_ShouldReturnEmptyList_WhenActionIsNotRefund()
    {
        // Arrange
        const string action = "CAL";

        // Act
        var result = await ticketInfoService.GetTicketsByAction(MatchId, SportType, action);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTicketsByAction_ShouldReturnTickets_WhenActionIsRefund()
    {
        // Arrange
        const string action = ActionName.Refund;
        var tickets = new List<TicketSettleInfo> { new() };
        repository.FetchAsync<TicketSettleInfo>(Arg.Any<TicketSettleCriteria>()).Returns(tickets);

        var sites = new List<SiteDb>
        {
            new SiteDb { SiteDbGroupId = 1, SiteName = "Site1" },
        };

        siteDbClientService.GetActiveSiteDbAsync().Returns(sites);

        // Act
        var result = await ticketInfoService.GetTicketsByAction(MatchId, SportType, action);

        // Assert
        Assert.Single(result);
        Assert.Equal(tickets, result);
    }

    [Fact]
    public async Task GetTicketsByTransIds_ShouldReturnEmptyList_WhenNoTicketsProvided()
    {
        // Arrange
        var tickets = new List<TicketSettleInfo>();

        // Act
        var result = await ticketInfoService.GetTicketsByTransIds(MatchId, SportType, tickets);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTicketsByTransIds_ShouldReturnTickets_WhenTicketsProvided()
    {
        // Arrange
        var tickets = new List<TicketSettleInfo> { new() { TransId = 123 } };
        var expectedTickets = new List<TicketSettleInfo> { new() };
        repository.FetchAsync<TicketSettleInfo>(Arg.Any<TicketSettleCriteria>()).Returns(expectedTickets);
        var sites = new List<SiteDb>
        {
            new SiteDb { SiteDbGroupId = 1, SiteName = "Site1" },
        };

        siteDbClientService.GetActiveSiteDbAsync().Returns(sites);

        // Act
        var result = await ticketInfoService.GetTicketsByTransIds(MatchId, SportType, tickets);

        // Assert
        Assert.Equal(expectedTickets, result);
    }

    [Fact]
    public async Task GetTicketsAllSites_ShouldReturnCombinedTicketsFromAllSites()
    {
        // Arrange
        const string action = ActionName.Refund;
        var ticketsFromSite1 = new List<TicketSettleInfo> { new() { TransId = 123 } };
        var ticketsFromSite2 = new List<TicketSettleInfo> { new() { TransId = 456 } };
        var sites = new List<SiteDb>
        {
            new SiteDb { SiteDbGroupId = 1, SiteName = "Site1" },
            new SiteDb { SiteDbGroupId = 2, SiteName = "Site2" }
        };

        siteDbClientService.GetActiveSiteDbAsync().Returns(sites);

        repository.FetchAsync<TicketSettleInfo>(Arg.Is<TicketSettleCriteria>(c => c.DbServer == "Site1")).Returns(ticketsFromSite1);
        repository.FetchAsync<TicketSettleInfo>(Arg.Is<TicketSettleCriteria>(c => c.DbServer == "Site2")).Returns(ticketsFromSite2);

        // Act
        var result = await ticketInfoService.GetTicketsByAction(MatchId, SportType, action);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.TransId == 123);
        Assert.Contains(result, t => t.TransId == 456);
    }
}