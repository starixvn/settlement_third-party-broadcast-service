using Fanex.Settlement.ScaleOut.Request;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

public class PinGoalServiceTests
{
    private readonly CalculateService calculateService;
    private readonly ScaleOutSettleService scaleOutSettleService;
    private readonly MatchService matchService;
    private readonly XmlConvertService xmlConvertService;
    private readonly PinGoalServiceImp service;

    public PinGoalServiceTests()
    {
        var logDirectlyService = Substitute.For<LogDirectlyService>();
        calculateService = Substitute.For<CalculateService>();
        scaleOutSettleService = Substitute.For<ScaleOutSettleService>();
        matchService = Substitute.For<MatchService>();
        xmlConvertService = Substitute.For<XmlConvertService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new PinGoalServiceImp(scaleOutSettleService, mapper, calculateService, logDirectlyService, matchService, xmlConvertService);
    }

    [Fact]
    public async Task Settle_ValidateFailed_StopProcess()
    {
        var resultFailed = new ResultMessage
        {
            ErrorCode = 99
        };
        matchService.GetMatchInfo(Arg.Any<int>()).Returns(new MatchInfo());
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(resultFailed);

        var result = await service.Settle(new PinGoalRequest());

        Assert.Equal(resultFailed.ErrorCode, result.ErrorCode);
        await scaleOutSettleService.DidNotReceive().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_DoSettle()
    {
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage());
        matchService.GetMatchInfo(Arg.Any<int>()).Returns(new MatchInfo());
        await service.Settle(new PinGoalRequest());

        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<PinGoalCriteria>());
    }

    [Fact]
    public async Task Settle_WhenHtNotSettled_ReturnsError()
    {
        // Arrange
        var matchInfo = new MatchInfo { LeagueGroupId = 252 };
        var request = new PinGoalRequest
        {
            MatchId = 123,
            Action = ActionName.Calculate,
            SelectedResources = "<Resources><Items><Item><Id>1</Id><Home>2</Home><Away>1</Away></Item></Items></Resources>"
        };

        matchService.GetMatchInfo(request.MatchId).Returns(matchInfo);
        xmlConvertService.Deserialize<ResourcesModel>(request.SelectedResources)
            .Returns(new ResourcesModel
            {
                Items = [new ResourceItemModel { Id = "00", Home = "2", Away = "1" }]
            });
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage());

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(1, result.ErrorCode);
        Assert.Equal("Failed: HT has not been settled", result.ErrorMessage);
    }

    [Fact]
    public async Task Settle_WhenHtScoreGreaterThanFt_ReturnsError()
    {
        // Arrange
        var matchInfo = new MatchInfo
        {
            LeagueGroupId = 252,
            HtHomeScore = 3,
            HtAwayScore = 2,
            FinalHomeScore = 2,
            FinalAwayScore = 1
        };

        var request = new PinGoalRequest
        {
            MatchId = 123,
            Action = ActionName.Calculate,
            SelectedResources = "<Resources><Items><Item><Id>1</Id><Home>3</Home><Away>2</Away></Item></Items></Resources>"
        };

        matchService.GetMatchInfo(request.MatchId).Returns(matchInfo);
        xmlConvertService.Deserialize<ResourcesModel>(request.SelectedResources)
            .Returns(new ResourcesModel
            {
                Items = [new ResourceItemModel { Id = "01", Home = "3", Away = "2" }]
            });
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage());

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(1, result.ErrorCode);
        Assert.Equal("Cannot calculate due to FT scores are less than HT Score. Please recheck entered values.", result.ErrorMessage);
    }

    [Fact]
    public async Task Settle_WhenValidScores_ProceedsWithSettlement()
    {
        // Arrange
        var matchInfo = new MatchInfo
        {
            LeagueGroupId = 252,
            HtHomeScore = 1,
            HtAwayScore = 1,
            FinalHomeScore = 2,
            FinalAwayScore = 1
        };

        var request = new PinGoalRequest
        {
            MatchId = 123,
            Action = ActionName.Calculate,
            SelectedResources = "<Resources><Items><Item><Id>1</Id><Home>1</Home><Away>1</Away></Item></Items></Resources>"
        };

        matchService.GetMatchInfo(request.MatchId).Returns(matchInfo);
        xmlConvertService.Deserialize<ResourcesModel>(request.SelectedResources)
            .Returns(new ResourcesModel
            {
                Items = [new ResourceItemModel { Id = "00", Home = "2", Away = "1" }]
            });
        scaleOutSettleService.ValidateEventAndTicket(Arg.Any<EventTicketRequest>()).Returns(new ResultMessage());
        calculateService.Execute(Arg.Any<PinGoalCriteria>()).Returns(new Result());

        // Act
        var result = await service.Settle(request);

        // Assert
        Assert.Equal(0, result.ErrorCode);
        await scaleOutSettleService.Received().UpdateMatchInfo(Arg.Any<UpdateMatchInfoRequest>());
        await calculateService.Received().Execute(Arg.Any<PinGoalCriteria>());
    }
}