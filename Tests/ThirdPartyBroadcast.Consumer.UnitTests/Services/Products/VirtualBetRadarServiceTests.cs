namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using AutoMapper;
using Criteria.VirtualBetRadar;
using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Microsoft.Extensions.Options;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualBetRadarServiceTests
{
    private readonly CalculateService calculateService;

    private readonly VirtualBetRadarService service;

    private readonly MatchService matchService;

    private readonly AnnouncementService announcementService;

    public VirtualBetRadarServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        var repository = Substitute.For<IDynamicRepository>();
        matchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();
        var sendMailService = Substitute.For<SendMailService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);

        var appSettingsKey = new AppSettingsKey { SiteDbServer = ["01", "02"], MailFunction = new MailFunction { VirtualSport = new MailInfo { CreditMailId = 103 } } };
        var options = Options.Create(appSettingsKey);

        service = new VirtualBetRadarServiceImp(repository, sendMailService, mapper, calculateService, matchService, announcementService, options);
    }

    [Fact]
    public async Task Settle_InValid_StopProcessAndReturn()
    {
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>())
            .Returns(new Result { ErrorCode = 1 });
        var result = await service.Settle(new VirtualBetRadarRequest());

        Assert.True(result.ErrorCode > 0);
    }

    [Fact]
    public async Task Settle_ValidationPassed_InvokeAllMethod()
    {
        var matchInfo = new MatchInfo
        {
            MatchId = 1,
            RefDataService = new RefDataServiceImp(new RemotingRefService())
        };

        matchService.GetMatchInfo(Arg.Any<int>()).Returns(matchInfo);
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>()).Returns(new Result());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(new Result());
        await service.Settle(new VirtualBetRadarRequest
        {
            Action = "CAL",
            MatchId = 1,
            Fta = 0,
            Fth = 1,
            Hta = 1,
            Hth = 1
        });

        Received.InOrder(() =>
        {
            matchService.GetMatchInfo(Arg.Any<int>());
            matchService.GetTicketsByAction(Arg.Any<int>(), Arg.Any<string>());
            matchService.UpdateMatchInfoBetRadar(Arg.Any<VirtualBetRadarRequest>());
            matchService.WriteLogBetRadar(Arg.Any<VirtualBetRadarRequest>());
            calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>());
            announcementService.Handle(Arg.Any<List<TicketSettleInfo>>(), Arg.Any<MatchInfo>(), Arg.Any<int>());
        });
    }
}