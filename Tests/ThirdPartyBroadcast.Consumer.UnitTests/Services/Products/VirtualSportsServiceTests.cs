namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using Criteria.VirtualSports;
using Fanex.Settlement.ScaleOut.Request;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualSportsServiceTests
{
    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly IDynamicRepository repository;

    private readonly VirtualSportsService service;

    private readonly LogDirectlyService logDirectlyService;

    public VirtualSportsServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleThirdPartyService = Substitute.For<ScaleOutSettleThirdPartyService>();
        repository = Substitute.For<IDynamicRepository>();
        logDirectlyService = Substitute.For<LogDirectlyService>();
        var matchService = Substitute.For<MatchService>();
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

        repository.GetAsync<MatchInfo>(Arg.Any<VirtualSportsGetMatchInfoCriteria>()).Returns(new MatchInfo { MatchId = request.MatchId });

        await service.Settle(request);

        await calculateService.Received().Execute(Arg.Any<VirtualSportsCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<VirtualSportsUpdateMatchCriteria>());
        await scaleOutSettleThirdPartyService.Received()
            .UpdateMatchInfo3rd(Arg.Is<UpdateMatchInfo3RdRequest>(item => item.Command == command && item.FTAway == away && item.FTHome == home));
        await logDirectlyService.Received().WriteLog(Arg.Any<int>(), Arg.Any<string>());
    }
}