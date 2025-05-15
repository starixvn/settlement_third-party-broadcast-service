using Fanex.SiteDbConnectionManager.Client.Models;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria.VirtualSports;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualSportsServiceTests
{
    private readonly CalculateService calculateService;

    private readonly MatchService matchService;

    private readonly AnnouncementService announcementService;

    private readonly TicketInfoService ticketInfoService;

    private readonly VirtualSportsServiceImp service;

    public VirtualSportsServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        matchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();
        ticketInfoService = Substitute.For<TicketInfoService>();
        var recipientMessageService = Substitute.For<RecipientMessageService>();
        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        service = new VirtualSportsServiceImp(matchService, mapper, calculateService, announcementService, recipientMessageService, ticketInfoService);
    }

    [Fact]
    public async Task Settle_ValidatePassed_InvokeAllMethod()
    {
        matchService.GetMatchInfo(Arg.Any<int>()).Returns(new MatchInfo { EventStatus = string.Empty });
        matchService.GetAllTicketsVirtualSports(Arg.Any<int>()).Returns((new List<TicketInfo>(), new List<TicketInfo>()));

        await service.Settle(new VirtualSoccerRequest());

        Received.InOrder(() =>
        {
            matchService.GetMatchInfo(Arg.Any<int>());
            ticketInfoService.GetTicketsByAction(Arg.Any<int>(), Arg.Any<short>(), Arg.Any<string>());
            matchService.UpdateMatchInfoVirtualSports(Arg.Any<VirtualSoccerRequest>());
            matchService.WriteLogVirtualSports(Arg.Any<VirtualSoccerRequest>());
            calculateService.Execute(Arg.Any<VirtualSportsCriteria>(), Arg.Any<SiteDbPendingCommand>());
            announcementService.SendAnnounceRefund(Arg.Any<List<TicketSettleInfo>>(), Arg.Any<MatchInfo>(), Arg.Any<int>());
        });
    }
}