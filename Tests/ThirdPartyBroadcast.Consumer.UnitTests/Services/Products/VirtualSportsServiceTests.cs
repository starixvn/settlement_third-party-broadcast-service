namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria.VirtualSports;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualSportsServiceTests
{
    private readonly CalculateService calculateService;

    private readonly VirtualSportsService service;

    private readonly MatchService matchService;

    private readonly AnnouncementService announcementService;

    public VirtualSportsServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        matchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        var appSettingsKey = new AppSettingsKey { SiteDbServer = ["01", "02"], MailFunction = new MailFunction { VirtualSport = new MailInfo { CreditMailId = 103 } } };
        var options = Options.Create(appSettingsKey);

        var sendMailService = Substitute.For<SendMailService>();

        service = new VirtualSportsServiceImp(matchService, mapper, calculateService, announcementService, sendMailService, options);
    }

    [Fact]
    public async Task Settle_ValidatePassed_InvokeAllMethod()
    {
        matchService.GetMatchInfoVirtualSports(Arg.Any<int>()).Returns(new MatchInfo { EventStatus = string.Empty });
        matchService.GetAllTicketsVirtualSports(Arg.Any<int>()).Returns((new List<TicketInfo>(), new List<TicketInfo>()));

        await service.Settle(new VirtualSoccerRequest());

        Received.InOrder(() =>
        {
            matchService.GetMatchInfoVirtualSports(Arg.Any<int>());
            matchService.GetTicketsByAction(Arg.Any<int>(), Arg.Any<string>());
            matchService.UpdateMatchInfoVirtualSports(Arg.Any<VirtualSoccerRequest>());
            matchService.WriteLogVirtualSports(Arg.Any<VirtualSoccerRequest>());
            calculateService.Execute(Arg.Any<VirtualSportsCriteria>());
            announcementService.Handle(Arg.Any<List<TicketSettleInfo>>(), Arg.Any<MatchInfo>(), Arg.Any<int>());
        });
    }
}