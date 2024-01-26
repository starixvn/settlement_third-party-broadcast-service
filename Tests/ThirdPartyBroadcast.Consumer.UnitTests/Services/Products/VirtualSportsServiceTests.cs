namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria.VirtualSports;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualSportsServiceTests
{
    private readonly CalculateService calculateService;

    private readonly VirtualSportsService service;

    private readonly MatchService matchService;

    public VirtualSportsServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        matchService = Substitute.For<MatchService>();
        var announcementService = Substitute.For<AnnouncementService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        var appSettingsKey = new AppSettingsKey { SiteDbServer = new List<string> { "01", "02" }, MailFunction = new MailFunction { VirtualSport = new MailInfo { CreditMailId = 103 } } };
        var options = Options.Create(appSettingsKey);

        var sendMailService = Substitute.For<SendMailService>();

        service = new VirtualSportsServiceImp(matchService, mapper, calculateService, announcementService, sendMailService, options);
    }

    [Theory]
    [InlineData(ActionName.Calculate, 1, 1, SpName.VirtualSports + "-CAL")]
    [InlineData(ActionName.Refund, null, null, SpName.VirtualSports + "-REFUND")]
    public async Task Settle_ValidatePassed_DoSettle(string action, int? home, int? away, string command)
    {
        var request = new VirtualSoccerRequest
        {
            Action = action,
            Fta = away,
            Fth = home
        };

        matchService.GetMatchInfoVirtualSports(Arg.Any<int>()).Returns(new MatchInfo { MatchId = request.MatchId, EventStatus = string.Empty });
        matchService.GetAllTicketsVirtualSports(Arg.Any<int>()).Returns((new List<TicketInfo>(), new List<TicketInfo>()));

        await service.Settle(request);

        await calculateService.Received().Execute(Arg.Any<VirtualSportsCriteria>());
        await matchService.Received().UpdateMatchInfoVirtualSports(Arg.Any<VirtualSoccerRequest>());
        await matchService.Received().WriteLogVirtualSports(Arg.Any<VirtualSoccerRequest>());
    }
}