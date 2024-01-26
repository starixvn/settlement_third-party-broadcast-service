using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products;

using AutoMapper;
using Criteria.VirtualBetRadar;
using Fanex.Data.Repository;
using ThirdPartyBroadcast.Consumer.Services;

public class VirtualBetRadarServiceTests
{
    private readonly CalculateService calculateService;

    private readonly IDynamicRepository repository;

    private readonly VirtualBetRadarService service;

    private readonly LogDirectlyService logService;

    private readonly MatchService matchService;

    private readonly AnnouncementService announcementService;

    public VirtualBetRadarServiceTests()
    {
        calculateService = Substitute.For<CalculateService>();
        repository = Substitute.For<IDynamicRepository>();
        logService = Substitute.For<LogDirectlyService>();
        matchService = Substitute.For<MatchService>();
        announcementService = Substitute.For<AnnouncementService>();

        var mapperConfig = new MapperConfigurationExpression();
        mapperConfig.AddProfile<ThirdPartyBroadcastMappingProfile>();
        var config = new MapperConfiguration(mapperConfig);
        var mapper = new Mapper(config);
        service = new VirtualBetRadarServiceImp(repository, mapper, calculateService, logService, matchService, announcementService);
    }

    [Theory]
    [InlineData(ActionName.Calculate, false, false, true, true, 0)]
    [InlineData(ActionName.Calculate, true, false, false, false, 2)]
    [InlineData(ActionName.Calculate, false, true, false, false, 2)]
    [InlineData(ActionName.Calculate, true, false, true, false, 10)]
    [InlineData(ActionName.Calculate, true, false, false, true, 10)]
    [InlineData(ActionName.Refund, true, false, false, true, 10)]
    public async Task Settle_ValidatePassed_CallSettleWithCorrectNotifyStatus(string action, bool calculate, bool calRefund, bool multiple, bool refund, int expected)
    {
        var request = new VirtualBetRadarRequest
        {
            Action = action,
        };
        var matchStatus = new MatchStatus
        {
            Refund = refund,
            Multiple = multiple,
            CalRefund = calRefund,
            Calculate = calculate
        };

        var mapperMock = Substitute.For<IMapper>();
        var serviceMock = new VirtualBetRadarServiceImp(repository, mapperMock, calculateService, logService, matchService, announcementService);
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>()).Returns(new Result());
        await repository.ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(new Result());
        repository.GetAsync<MatchStatus>(Arg.Any<GetMatchStatusCriteria>()).Returns(matchStatus);
        mapperMock.Map<VirtualBetRadarRequest, VirtualBetRadarCriteria>(request).Returns(new VirtualBetRadarCriteria { DbServer = "DBTXN" });

        //Action
        await serviceMock.Settle(request);

        //Assert
        await calculateService.Received().Execute(Arg.Any<VirtualBetRadarCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassed_SettleSuccessAndWriteLog()
    {
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>())
            .Returns(new Result());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(new Result());
        repository.GetAsync<MatchStatus>(Arg.Any<GetMatchStatusCriteria>()).Returns(new MatchStatus());

        await service.Settle(new VirtualBetRadarRequest());

        await repository.Received().ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
        await calculateService.Received().Execute(Arg.Any<VirtualBetRadarCriteria>());
        await repository.Received().ExecuteAsync(Arg.Any<WriteLogCriteria>());
    }

    [Fact]
    public async Task Settle_ValidatePassedAndSettleFailed_WriteCorrectLog()
    {
        var resultSettle = new Result
        {
            ErrorCode = 1,
            ErrorMessage = "Error Test"
        };
        calculateService.ExecuteValidationMultiServer(Arg.Any<BetRadarValidationCriteria>())
            .Returns(new Result());
        calculateService.Execute(Arg.Any<VirtualBetRadarCriteria>()).Returns(resultSettle);
        repository.GetAsync<MatchStatus>(Arg.Any<GetMatchStatusCriteria>()).Returns(new MatchStatus());

        //action
        await service.Settle(new VirtualBetRadarRequest());
        const string message = "VS BetRadar - Calculate (CAL) Error Test";

        //assert
        await repository.Received().ExecuteAsync(Arg.Any<BetRadarUpdateMatchCriteria>());
        await calculateService.Received().Execute(Arg.Any<VirtualBetRadarCriteria>());
        await repository.Received().ExecuteAsync(Arg.Is<WriteLogCriteria>(item => item.Remark == message));
    }
}