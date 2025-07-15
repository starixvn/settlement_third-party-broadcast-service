namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using AutoMapper;
using Criteria.VirtualBetRadar;
using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Fanex.SiteDbConnectionManager.Client.Models;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualBetRadarServiceTests
{
    private readonly CalculateService calculateService;

    private readonly MatchService matchService;

    private readonly AnnouncementService announcementService;

    private readonly TicketInfoService ticketInfoService;

    private readonly IMapper mapper;

    private readonly VirtualBetRadarServiceImp service;

    public VirtualBetRadarServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        var repository = Substitute.For<IDynamicRepository>();
        matchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();
        var recipientMessageService = Substitute.For<RecipientMessageService>();
        ticketInfoService = Substitute.For<TicketInfoService>();
        mapper = Substitute.For<IMapper>();

        service = new VirtualBetRadarServiceImp(repository, mapper, calculateService, matchService, announcementService, recipientMessageService, ticketInfoService);
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
        var request = new VirtualBetRadarRequest
        {
            Action = "CAL",
            MatchId = 1,
            Fta = 0,
            Fth = 1,
            Hta = 1,
            Hth = 1
        };

        mapper.Map<BetRadarValidationCriteria>(request).Returns(new BetRadarValidationCriteria());
        mapper.Map<VirtualBetRadarCriteria>(request).Returns(new VirtualBetRadarCriteria());

        matchService.GetMatchInfo(Arg.Any<int>()).Returns(matchInfo);
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>()).Returns(new Result());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>(), Arg.Any<SiteDbPendingCommand>()).Returns(new Result());
        await service.Settle(request);

        Received.InOrder(() =>
        {
            matchService.GetMatchInfo(Arg.Any<int>());
            ticketInfoService.GetTicketsByAction(Arg.Any<int>(), Arg.Any<short>(), Arg.Any<string>());
            matchService.UpdateMatchInfoBetRadar(Arg.Any<VirtualBetRadarRequest>());
            matchService.WriteLogBetRadar(Arg.Any<VirtualBetRadarRequest>());
            calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>(), Arg.Any<SiteDbPendingCommand>());
            announcementService.SendAnnounceRefund(Arg.Any<List<TicketSettleInfo>>(), Arg.Any<MatchInfo>(), Arg.Any<int>());
        });
    }
}